using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenCharacterFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform nosePoint;
    [SerializeField] private Transform penTipMarker;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeed = 10f;

    [Tooltip("Kuinka kaukana kyn‰n k‰rjen pit‰‰ olla nen‰n edess‰.")]
    [SerializeField] private float noseToPenDistance = 0.4f;

    [Header("World plane")]
    [Tooltip("Mill‰ Y-korkeudella hahmo liikkuu.")]
    [SerializeField] private float movementPlaneY = 0f;

    private Vector3 penWorldPosition;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        UpdatePenPosition();
        FollowPen();
    }

    private void UpdatePenPosition()
    {
        // Kyn‰ toimii Unityss‰ yleens‰ samalla tavalla kuin hiiren osoitin.
        Vector2 screenPosition = Input.mousePosition;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // Tyhj‰n tilan vaakasuora XZ-taso.
        Plane movementPlane = new Plane(
            Vector3.up,
            new Vector3(0f, movementPlaneY, 0f)
        );

        if (movementPlane.Raycast(ray, out float distance))
        {
            penWorldPosition = ray.GetPoint(distance);

            // Valinnainen pieni objekti n‰ytt‰m‰‰n kyn‰n k‰rjen sijainnin.
            if (penTipMarker != null)
                penTipMarker.position = penWorldPosition;
        }
    }

    private void FollowPen()
    {
        Vector3 direction = penWorldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        // Hahmon nen‰ k‰‰ntyy kyn‰‰ kohti.
        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        // Jos NosePoint on m‰‰ritelty, mitataan et‰isyys oikeasta nen‰st‰.
        Vector3 referencePosition =
            nosePoint != null ? nosePoint.position : transform.position;

        Vector3 noseToPen = penWorldPosition - referencePosition;
        noseToPen.y = 0f;

        // Liikutaan vain, jos kyn‰ on haluttua et‰isyytt‰ kauempana nen‰st‰.
        if (noseToPen.magnitude > noseToPenDistance)
        {
            transform.position +=
                transform.forward * moveSpeed * Time.deltaTime;
        }
    }
}