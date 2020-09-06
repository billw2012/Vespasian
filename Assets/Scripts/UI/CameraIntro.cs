using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(FollowCameraController))]
public class CameraIntro : MonoBehaviour
{
    public List<Transform> targets;
    public Transform player;
    FollowCameraController camController;
    public SimManager simMgrComponent;

    int currentTargetID;
    float smoothTimeStart;

    // Start is called before the first frame update
    void Start()
    {
        // Assertions
        Assert.IsTrue(this.player != null);
        Assert.IsTrue(this.simMgrComponent != null);

        this.camController = GetComponent<FollowCameraController>();

        this.smoothTimeStart = camController.smoothTime;

        if (this.targets.Count > 0)
        {
            this.currentTargetID = 0;
            this.camController.smoothTime = 0.9f;
            this.camController.SetTarget(this.targets[0]);
            this.camController.ForceFocusOnTarget();
            this.targets.Add(this.player);
        }
        else
        {
            // If targets are not specified, just ignore it and start the game
            this.startGame();
            this.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Iterate all targets till there are no targets
        if (camController.atTargetPosition)
        {
            if (this.currentTargetID == targets.Count-1)
            {
                // Animation is done, let's play now
                this.startGame();
            }
            else
            {
                // We have more objects to visit
                this.currentTargetID++;
                this.camController.SetTarget(this.targets[this.currentTargetID]);
            }
        }
    }

    void startGame()
    {
        this.simMgrComponent.enabled = true;
        //this.camController.SetTarget(this.player);
        this.camController.smoothTime = this.smoothTimeStart;
        this.camController.searchPointsOfInterest = true;
        this.camController.clampToCameraInnerArea = true;
    }
}
