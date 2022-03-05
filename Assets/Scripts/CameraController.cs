using Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("Camera-Control/CameraController")]
public class CameraController : MonoBehaviour
{
    #region EditorReferences

    [SerializeField, Tooltip("Velocity variable on the x-axis as the object rotates.")]
    private float xSpeed = 50.0f;
    [SerializeField, Tooltip("Velocity variable on the y-axis as the object rotates.")]
    private float ySpeed = 50.0f;
    [SerializeField, Tooltip("Speed variable used while zoom-in and zoom-out")]
    private float zoomRate = 0.5f;
    [SerializeField, Tooltip("Speed variable used when panning.")]
    private float panSpeed = 0.0025f;
    [SerializeField, Tooltip("Maximum value of the distance between 2 fingers while panning.")]
    private float panTouchesSpace = 10f;
    [SerializeField, Tooltip("Object to be followed by the camera.")]
    private Transform followObject = null;

    #endregion

    #region Values

    private bool onUI = false;
    private float xDeg = 0.0f;
    private float yDeg = 0.0f;
    private float calculatedDeltaZoom = 0;
    private float firstDistanceBetweenTouchesZoom = 0;
    private Vector3 desiredPos = Vector3.zero;
    private CinemachineVirtualCamera currentCam = null;

    #endregion

    private void Awake()
    {
        currentCam = GetComponent<CinemachineVirtualCamera>();

        currentCam.Priority = int.MaxValue;
        currentCam.Follow = followObject;

        xDeg = currentCam.transform.eulerAngles.y;
        yDeg = currentCam.transform.eulerAngles.x;
    }

    private void LateUpdate()
    {
        #region TouchIgnore
#if UNITY_ANDROID || UNITY_IOS

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
                onUI = IsPointerOverUIElement();
            else if (touch.phase == TouchPhase.Ended)
                onUI = false;
        }

#endif
        #endregion

        #region MouseIgnore
#if UNITY_EDITOR

        if (Input.GetMouseButtonDown(0))
            onUI = IsPointerOverUIElement();
        else if (Input.GetMouseButtonUp(0))
            onUI = false;

#endif
        #endregion

        if (onUI)
            return;

        #region TouchControl
#if UNITY_ANDROID || UNITY_IOS

        if (Input.touchCount == 1)
        {
            #region ObjLook

            xDeg += Input.touches[0].deltaPosition.x * xSpeed * 0.004f;
            yDeg -= Input.touches[0].deltaPosition.y * ySpeed * 0.004f;

            currentCam.transform.rotation = Quaternion.Euler(yDeg, xDeg, 0);

            #endregion
        }
        else if (Input.touchCount == 2)
        {
            var touch0 = Input.GetTouch(0);
            var touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
                firstDistanceBetweenTouchesZoom = Vector3.Distance(touch0.position, touch1.position);

            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                var distanceBetweenTouches = Vector3.Distance(touch0.position, touch1.position);
                calculatedDeltaZoom = distanceBetweenTouches - firstDistanceBetweenTouchesZoom;

                Ray ray = Camera.main.ScreenPointToRay((touch0.position + touch1.position) / 2);

                var deltaX0 = Input.touches[0].deltaPosition.x;
                var deltaX1 = Input.touches[1].deltaPosition.x;

                var deltaY0 = Input.touches[0].deltaPosition.y;
                var deltaY1 = Input.touches[1].deltaPosition.y;

                var calculatedDeltaX = Mathf.Abs(deltaX0 - deltaX1);
                var calculatedDeltaY = Mathf.Abs(deltaY0 - deltaY1);

                #region Pan

                if (calculatedDeltaX < panTouchesSpace && calculatedDeltaY < panTouchesSpace)
                {
                    Vector3 newPos = currentCam.transform.right * -Input.touches[0].deltaPosition.x * panSpeed;
                    newPos += currentCam.transform.up * -Input.touches[0].deltaPosition.y * panSpeed;
                    desiredPos += newPos;
                }

                #endregion

                #region Pinch

                firstDistanceBetweenTouchesZoom = distanceBetweenTouches;
                desiredPos += ray.direction * (zoomRate) * calculatedDeltaZoom / 150;

                #endregion

                followObject.position = Vector3.Lerp(followObject.position, desiredPos, Time.deltaTime * Mathf.Abs(calculatedDeltaZoom) * 1000);
            }
        }

#endif
        #endregion

        #region MouseControl
#if UNITY_EDITOR

        if (Input.GetMouseButton(0))
        {
            #region ObjLook

            xDeg += Input.GetAxis("Mouse X") * xSpeed * 0.1f;
            yDeg -= Input.GetAxis("Mouse Y") * ySpeed * 0.1f;

            currentCam.transform.rotation = Quaternion.Euler(yDeg, xDeg, 0);

            #endregion
        }
        else if (Input.GetMouseButton(1))
        {
            #region Pan

            Vector3 newPos = currentCam.transform.right * -Input.GetAxis("Mouse X") * panSpeed * 50;
            newPos += currentCam.transform.up * -Input.GetAxis("Mouse Y") * panSpeed * 50;
            followObject.position += newPos;

            #endregion
        }

        else if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            #region FreePinch

            followObject.position += currentCam.transform.forward * zoomRate * Input.GetAxis("Mouse ScrollWheel") * zoomRate * 10;

            #endregion
        }

#endif
        #endregion
    }

    #region IgnoreUI

    public static bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }
    ///Returns 'true' if we touched or hovering on Unity UI element.
    public static bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaycastResult = eventSystemRaysastResults[index];
            if (curRaycastResult.gameObject.layer == LayerMask.NameToLayer("UI") && curRaycastResult.gameObject.layer != 6)
            {
                return true;
            }
        }
        return false;
    }
    ///Gets all event systen raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }

    #endregion
}