using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GestureDrawer : MonoBehaviour
{

    public LineRenderer lineRenderer;
    public LineRenderer guideLineRenderer;
    public PlayerMovement character; // Hahmon liikkumisskripti

    [Header("Piirtoalustan asetukset")]
    public Transform drawingCanvas;
    public LayerMask drawingCanvasLayer; // Valitaan Inspectorissa k‰yttˆˆn otettu Layer
    public float offsetFromCanvas = 0.0f; // Et‰isyys alustasta jotta viiva ei ole alustan sis‰ll‰
    public float guideOffsetFromCanvas = 0.0f; // Alempana kuin pelaajan viiva

    [Header("Pyyhint‰asetukset")]
    public float eraseDelay = 5.0f; // Aika sekunneissa, jonka j‰lkeen viiva h‰vi‰‰

    private List<Vector3> drawnPoints = new List<Vector3>();
    private bool isDrawing = false;
    private Coroutine eraseCoroutine; // Pit‰‰ kirjaa k‰ynniss‰ olevasta ajastimesta


    // viiva tai mik‰ tahansa tavoitekuvio neliˆidyss‰ koordinaatistossa (0..1), nyt alhaalta ylˆs
    private List<Vector2> targetGesture = new List<Vector2>()
    {
        new Vector2(0.0f, 1.0f),
       
    };

    // Start is called before the first frame update
    void Start()
    {
        DrawGuide(targetGesture);
    }

    public void DrawGuide(List<Vector2> points)
    {

        if (guideLineRenderer == null || drawingCanvas == null) return;

        // Haetaan oletuskoko MeshFilterist‰ (Unityn Planella se on X: 10, Z: 10)
        MeshFilter meshFilter = drawingCanvas.GetComponent<MeshFilter>();
        Vector3 meshSize = (meshFilter != null) ? meshFilter.sharedMesh.bounds.size : new Vector3(10f, 0f, 10f);

        guideLineRenderer.positionCount = points.Count + 1;


        Vector3 initialworldPos = drawingCanvas.TransformPoint(Vector3.zero);
        initialworldPos += drawingCanvas.up * guideOffsetFromCanvas;

        guideLineRenderer.SetPosition(0, initialworldPos);

        for (int i = 0; i < points.Count; i++)
        {
            // 1. Muutetaan suhteellinen 0..1 koordinaatti Planen paikalliseksi koordinaatiksi (-0.5 .. 0.5)
            // Planen oletuskoko paikallisessa tilassa on -0.5 ... 0.5 X- ja Z-akseleilla
            float localX = points[i].x * meshSize.x;
            float localZ = points[i].y * meshSize.z; // K‰ytet‰‰n Z-akselia pystysuunnalle

            Vector3 localPos = new Vector3(localX, 0f, localZ);

            // 2. Muutetaan paikallinen piste 3D-maailmanpisteeksi
            Vector3 worldPos = drawingCanvas.TransformPoint(localPos);

            // 3. Lis‰t‰‰n pieni nosto pinnasta (normal-suuntaan)
            worldPos += drawingCanvas.up * guideOffsetFromCanvas;

            guideLineRenderer.SetPosition(i +1, worldPos);
        }
    }

    // Update is called once per frame
    void Update()
    {

        // Aloita piirt‰minen
        if (Input.GetMouseButtonDown(0))
        {

            // Jos edellinen pyyhint‰ajastin oli jo k‰ynniss‰, peruutetaan se,
            // jotta viiva ei pyyhkiytyisi kesken uuden piirron!
            if (eraseCoroutine != null)
            {
                StopCoroutine(eraseCoroutine);
            }


            isDrawing = true;
            drawnPoints.Clear();
            lineRenderer.positionCount = 0;
        }

        // Lis‰‰ pisteit‰, kun hiirt‰ liikutetaan
        if (isDrawing && Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Ammutaan s‰de hiiren kohdalta ja katsotaan osuuko se piirtoalustan Layeriin
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, drawingCanvasLayer))
            {
                // Nosteaan pistett‰ hieman pinnasta ylˆsp‰in (pinnan normaalin suuntaan),
                // jotta LineRenderer ei mene sekaisin Planen oman pinnan kanssa ("Z-fighting")
                //Vector3 pointOnCanvas = hit.point;
                Vector3 pointOnCanvas = hit.point + (hit.normal * offsetFromCanvas);

                if (drawnPoints.Count == 0 || Vector3.Distance(drawnPoints[drawnPoints.Count - 1], pointOnCanvas) > 0.1f)
                {
                    drawnPoints.Add(pointOnCanvas);
                    lineRenderer.positionCount = drawnPoints.Count;
                    lineRenderer.SetPosition(drawnPoints.Count - 1, pointOnCanvas);
                }
            }
        }

        // Lopeta piirt‰minen ja tarkista kuvio
        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            isDrawing = false;
            CheckGesture();

            // K‰ynnistet‰‰n 5 sekunnin ajastin piirron tyhjent‰miselle
            eraseCoroutine = StartCoroutine(EraseLineAfterDelay(eraseDelay));
        }

    }

    // Coroutine, joka odottaa halutun ajan ja tyhjent‰‰ viivan
    IEnumerator EraseLineAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Tyhjennet‰‰n pisteet ja LineRenderer
        drawnPoints.Clear();
        lineRenderer.positionCount = 0;
    }

    void CheckGesture()
    {
        if (drawnPoints.Count < 5) return; // Liian lyhyt viiva

        Debug.Log($"Aloituspiste 3D: {drawnPoints[0]} | Lopetuspiste 3D: {drawnPoints[drawnPoints.Count - 1]}");
        // Muutetaan 3D-pisteet 2D-pisteiksi tunnistusta varten
        List<Vector2> points2D = ConvertTo2D(drawnPoints);

        //List<Vector2> normalizedPoints = NormalizePoints(points2D);
        //List<Vector2> resampledPoints = Resample(normalizedPoints, targetGesture.Count);

        List<Vector2> canvasPoints = ConvertToCanvas(drawnPoints);
        //List<Vector2> resampledPoints = Resample(canvasPoints, targetGesture.Count);

        // TULOSTETAAN ENSIMMƒINEN JA VIIMEINEN PISTE CONSOLEEN:
        Debug.Log($"Aloituspiste 2D: {canvasPoints[0]} | Lopetuspiste 2D: {canvasPoints[canvasPoints.Count - 1]}");

        float accuracy = CalculateAccuracy(canvasPoints, targetGesture);

        Debug.Log($"Kuvion tarkkuus: {accuracy * 100}%");

        if (accuracy >= 0.8f)
        {
            character.Jump();
        }
    }

    // Muutetaan 3D-maailmanpisteet 2D-pisteiksi tunnistusta varten
    List<Vector2> ConvertTo2D(List<Vector3> points3D)
    {
        List<Vector2> points2D = new List<Vector2>();
        foreach (var p in points3D)
        {
            if (drawingCanvas != null)
            {
                // Muutetaan 3D-maailmanpiste Planen omaksi paikalliseksi (local) koordinaatiksi
                Vector3 localPos = drawingCanvas.InverseTransformPoint(p);
                points2D.Add(new Vector2(localPos.x, localPos.z));
            }
            else
            {

            
                // K‰ytet‰‰n t‰ss‰ esimerkiss‰ X- ja Y-akseleita. 
                // Jos Plane-alustasi on vaakatasossa (lattia), k‰yt‰ p.x ja p.z!
                points2D.Add(new Vector2(p.x, p.z));
            }
        }
        return points2D;
    }

    //KORJAUS; piirron koolla on v‰li‰, ei skaalata sit‰, t‰m‰ pois
    // Asteikotetaan pisteet 0..1 v‰lille, jotta piirron koolla ei ole v‰li‰
    /*
    List<Vector2> NormalizePoints(List<Vector2> points)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var p in points)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        float width = maxX - minX;
        float height = maxY - minY;
        float size = Mathf.Max(width, height); // S‰ilytet‰‰n kuvasuhde

        List<Vector2> normalized = new List<Vector2>();
        foreach (var p in points)
        {
            float x = (size == 0) ? 0 : (p.x - minX) / size;
            float y = (size == 0) ? 0 : (p.y - minY) / size;
            normalized.Add(new Vector2(x, y));
        }
        return normalized;
    }
    */

    List<Vector2> ConvertToCanvas(List<Vector3> points3D)
    {
        List<Vector2> canvasPoints = new List<Vector2>();

        if (drawingCanvas == null)
        {
            Debug.LogError("drawingCanvas EI OLE ASETETTU Inspectorissa!");
            return canvasPoints;
        }

        // Haetaan Planen skaalaukset Inspectorista (0.3 ja 0.3)
        float scaleX = drawingCanvas.transform.localScale.x;
        float scaleZ = drawingCanvas.transform.localScale.z;

        foreach (var p in points3D)
        {
            Vector3 localPos = drawingCanvas.InverseTransformPoint(p);
            float vaaka = localPos.x ;
            float pysty = localPos.z ;

            canvasPoints.Add(new Vector2(vaaka, pysty));
            Debug.LogWarning(pysty + ", " + vaaka);
        }

        /*
        MeshFilter meshFilter = drawingCanvas.GetComponent<MeshFilter>();

        Vector3 meshSize = (meshFilter != null) ? meshFilter.sharedMesh.bounds.size : new Vector3 (10f, 0, 10f);

        foreach (var p in points3D)
        {
            Vector3 localPos = drawingCanvas.InverseTransformPoint(p);
            float vaaka = (localPos.x / meshSize.x) + 0.5f;
            float pysty = (localPos.y / meshSize.y) + 0.5f;

            canvasPoints.Add(new Vector2(pysty, vaaka));
        }
        */

        return canvasPoints;
    }

    // Yksinkertaistetaan pisteiden m‰‰r‰ t‰sm‰‰m‰‰n tavoitetta, voi piirt‰‰ ympyr‰‰
    List<Vector2> Resample(List<Vector2> points, int targetCount)
    {
        List<Vector2> resampled = new List<Vector2>();
        float step = (float)(points.Count - 1) / (targetCount - 1);

        for (int i = 0; i < targetCount; i++)
        {
            int index = Mathf.RoundToInt(i * step);
            resampled.Add(points[Mathf.Clamp(index, 0, points.Count - 1)]);
        }
        return resampled;
    }

    // Vertaa kahden kuviopartion et‰isyyksi‰, tee uusiksi, eli lista vector vaan pekk‰ vektori mihin vertaa
    float CalculateAccuracy(List<Vector2> drawn, List<Vector2> target) //<---
    {
        float totalDistance = 0f;

        List<float> sectionDistances = new List<float>();
        List<float> sectionAccuracies = new List<float>();

        for (int i = 1; i < drawn.Count; i++)
        {
            float sectionDistance = Vector2.Distance(drawn[i -1], drawn[i]);
            sectionDistances.Add(sectionDistance);
            totalDistance += sectionDistance;
        }

        float virtualDistance = 0f;

        virtualDistance = Vector2.Distance(drawn[0], drawn[drawn.Count - 1]);

        if (totalDistance > virtualDistance * 1.1f)
        {
            
            Debug.LogWarning("invalid gesture lenght");
            return 0f;
        }


        for (int i = 1; i < drawn.Count; i++)
        {
            Vector2 direction = drawn[i] - drawn[i -1];
            Vector2 normalizedDirection = direction.normalized;

            float sectionAccuracy = 1 - Vector2.Distance(normalizedDirection, target[0]);
            sectionAccuracies.Add(sectionAccuracy);

            Debug.Log(drawn[i]);
        }

        float accuracy = 0f;

        for (int i = 0; i < sectionDistances.Count; i++)
        {
            accuracy += sectionAccuracies[i] * (sectionDistances[i] / totalDistance);
        }

        


        return Mathf.Clamp01(accuracy);
    }


}
