﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(FollowCameraController))]
public class CameraIntro : MonoBehaviour
{
    [Tooltip("Targets for the camera flyby, in order (leave empty to automatically use Objectives in the scene)")]
    public List<Transform> targets;

    FollowCameraController camController;
    int currentTargetID;
    float smoothTimeStart;

    void Start()
    {
        this.camController = this.GetComponent<FollowCameraController>();

        this.smoothTimeStart = this.camController.smoothTime;

        var player = FindObjectOfType<PlayerLogic>().transform;
        if (this.targets.Count == 0)
        {
            this.targets = FindObjectsOfType<Objective>()
                .Select(o => o.target)
                // TODO: solve traveling salesman problem, then order the objectives better
                .OrderBy(o => Vector2.Distance(player.transform.position, o.position))
                .ToList();
        }

        var simManager = FindObjectOfType<SimManager>();
        if (simManager != null)
        {
            simManager.enabled = false;
        }
        this.camController.smoothTime = 0.9f;
        // Start at player
        this.currentTargetID = -1;
        this.camController.SetTarget(player);
        this.camController.ForceFocusOnTarget();
        // End at player
        this.targets.Add(player);
    }

    void Update()
    {
        // Iterate all targets till there are no targets
        if (this.camController.atTargetPosition)
        {
            if (this.currentTargetID == this.targets.Count-1)
            {
                // Animation is done, let's play now
                this.StartGame();
            }
            else
            {
                // We have more objects to visit
                this.currentTargetID++;
                this.camController.SetTarget(this.targets[this.currentTargetID]);
            }
        }
    }

    void StartGame()
    {
        //this.camController.SetTarget(this.player);
        this.camController.smoothTime = this.smoothTimeStart;
        this.camController.searchPointsOfInterest = true;
        this.camController.clampToCameraInnerArea = true;

        var simManager = FindObjectOfType<SimManager>();
        if (simManager != null)
        {
            simManager.enabled = true;
        }
        this.enabled = false;
    }
}
