using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoHelper : MonoBehaviour
{
    public Transform obstacleCourse;
    public Transform racingTrack;

    public GameObject firstPersonCamera;

    public KartController kart;
    public KartModelController rootModel;
    public Transform oldModel;
    public Transform newModel;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            kart.transform.position = obstacleCourse.position;
            kart.currentRotation = obstacleCourse.eulerAngles.y;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            kart.transform.position = racingTrack.position;
            kart.currentRotation = racingTrack.eulerAngles.y;
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            rootModel.model = newModel;
            newModel.gameObject.SetActive(true);
            oldModel.gameObject.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            rootModel.model = oldModel;
            newModel.gameObject.SetActive(false);
            oldModel.gameObject.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            firstPersonCamera.SetActive(!firstPersonCamera.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.Comma))
        {
            QualitySettings.DecreaseLevel(true);
        }
        if (Input.GetKeyDown(KeyCode.Period))
        {
            QualitySettings.IncreaseLevel(true);
        }
    }
}
