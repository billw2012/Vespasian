using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(FollowCameraController))]
public class CameraIntro : MonoBehaviour
{
    [Tooltip("Targets for the camera flyby, in order (leave empty to automatically use Objectives in the scene)")]
    public List<Transform> targets;

    private FollowCameraController camController;
    private int currentTargetID;
    private float smoothTimeStart;
    private float nextTargetTime = 0;

    private const float PauseTime = 1f;

    private void Start()
    {
        this.camController = this.GetComponent<FollowCameraController>();

        this.smoothTimeStart = this.camController.smoothTime;

        var player = ComponentCache.FindObjectOfType<PlayerController>();
        if(player == null)
        {
            this.StartGame();
            return;
        }
        if (!this.targets.Any())
        {
            this.targets = ComponentCache.FindObjectsOfType<PositionalObjective>()
                .Select(o => o.target)
                // TODO: solve traveling salesman problem, then order the objectives better
                .OrderBy(o => Vector2.Distance(player.transform.position, o.position))
                .ToList();
            // Just forget the intro if there are no objectives at all
            if(!this.targets.Any())
            {
                this.StartGame();
                return;
            }
        }

        var simManager = ComponentCache.FindObjectOfType<Simulation>();
        if (simManager != null)
        {
            simManager.enabled = false;
        }
        this.camController.smoothTime = 0.9f;
        // Start at player
        this.currentTargetID = -1;
        this.camController.SetTarget(player.transform);
        this.camController.ForceFocusOnTarget();
        // End at player
        this.targets.Add(player.transform);
        this.nextTargetTime = 0;
    }

    private void Update()
    {
        // Iterate all targets till there are no targets
        if (this.camController.atTargetPosition)
        {
            if (this.nextTargetTime == 0)
            {
                this.nextTargetTime = Time.time + PauseTime;
            }
            else if (this.nextTargetTime < Time.time)
            {
                this.nextTargetTime = 0;
                if (this.currentTargetID == this.targets.Count - 1)
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
    }

    private void StartGame()
    {
        //this.camController.SetTarget(this.player);
        this.camController.smoothTime = this.smoothTimeStart;
        this.camController.searchPointsOfInterest = true;
        this.camController.clampToCameraInnerArea = true;

        var simManager = ComponentCache.FindObjectOfType<Simulation>();
        if (simManager != null)
        {
            simManager.enabled = true;
        }
        this.enabled = false;
    }
}
