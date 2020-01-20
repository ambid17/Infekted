﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public LayerMask layersToIgnore;
    public BoxCollider myCollider;

    //The velocity applied at the end of every physics frame
    public Vector3 newVelocity;

    [SerializeField]
    private bool grounded;
    [SerializeField]
    private bool surfing;
    [SerializeField]
    private bool crouching;
    [SerializeField]
    private bool wasCrouching;

    private void Start()
    {
        myCollider = GetComponent<BoxCollider>();
    }

    private void FixedUpdate()
    {
        CheckCrouch();
        ApplyGravity();
        CheckGrounded();
        CheckJump();

        var inputVector = GetWorldSpaceInputVector();
        var wishDir = inputVector.normalized;
        var wishSpeed = inputVector.magnitude;

        if (grounded)
        {
            ApplyGroundAcceleration(wishDir, wishSpeed, PlayerConstants.normalSurfaceFriction);
            ClampVelocity(PlayerConstants.MoveSpeed);
            ApplyFriction();
        }
        else
        {
            ApplyAirAcceleration(wishDir, wishSpeed);
        }

        ClampVelocity(PlayerConstants.MaxVelocity);

        transform.position += newVelocity * Time.deltaTime;

        ResolveCollisions();
    }

    private void CheckCrouch()
    {
        wasCrouching = crouching;

        if (Input.GetKey(HotKeyManager.instance.GetKeyFor(PlayerConstants.Crouch)))
        {
            crouching = true;
            myCollider.size = PlayerConstants.CrouchingBoxColliderSize;
        }
        else
        {
            crouching = false;

            if (grounded && wasCrouching)
            {
                transform.position += new Vector3(0, 1, 0);
            }

            myCollider.size = PlayerConstants.BoxColliderSize;
        }
    }

    private void ApplyGravity()
    {
        if (!grounded)
        {
            newVelocity.y -= PlayerConstants.Gravity * Time.deltaTime;
        }
    }

    private void CheckGrounded()
    {
        surfing = false;

        Vector3 extents = PlayerConstants.BoxCastExtents;
        if (crouching)
        {
            extents = PlayerConstants.CrouchingBoxCastExtents;
        }

        var hits = Physics.BoxCastAll(center: myCollider.bounds.center,
            halfExtents: extents,
            direction: -transform.up,
            orientation: Quaternion.identity,
            maxDistance: PlayerConstants.BoxCastDistance,
            layerMask: layersToIgnore
            );

        var wasGrounded = grounded;
        var validHits = hits
            .ToList()
            .FindAll(hit => hit.normal.y >= 0.7f)
            .OrderBy(hit => hit.distance)
            .Where(hit => !hit.collider.isTrigger);

        grounded = validHits.Count() > 0;

        if (grounded)
        {
            var closestHit = validHits.First();

            //If the ground is NOT perfectly flat, slide across it
            if (closestHit.normal.y < 1)
            {
                ClipVelocity(closestHit.normal);
            }
            else
            {
                newVelocity.y = 0;
            }
        }
        else
        {
            //Find the closest collision where the slope is at least 45 degrees
            var surfHits = hits.ToList().FindAll(x => x.normal.y < 0.7f).OrderBy(x => x.distance);
            if (surfHits.Count() > 0)
            {
                transform.position += surfHits.First().normal * 0.02f;
                ClipVelocity(surfHits.First().normal);
                surfing = true;
            }
        }
    }

    private void CheckJump()
    {
        if (grounded && Input.GetKey(HotKeyManager.instance.GetKeyFor(PlayerConstants.Jump)))
        {
            newVelocity.y += crouching ? PlayerConstants.CrouchingJumpPower : PlayerConstants.JumpPower;
            grounded = false;
        }
    }

    private Vector3 GetWorldSpaceInputVector()
    {
        float moveSpeed = crouching ? PlayerConstants.CrouchingMoveSpeed : PlayerConstants.MoveSpeed;

        var inputVelocity = GetInputVelocity(moveSpeed);
        if (inputVelocity.magnitude > moveSpeed)
        {
            inputVelocity *= moveSpeed / inputVelocity.magnitude;
        }

        //Get the velocity vector in world space coordinates
        return transform.TransformDirection(inputVelocity);
    }

    private Vector3 GetInputVelocity(float moveSpeed)
    {
        KeyCode left = HotKeyManager.instance.GetKeyFor(PlayerConstants.Left);
        KeyCode right = HotKeyManager.instance.GetKeyFor(PlayerConstants.Right);
        KeyCode forward = HotKeyManager.instance.GetKeyFor(PlayerConstants.Forward);
        KeyCode back = HotKeyManager.instance.GetKeyFor(PlayerConstants.Back);

        float horizontalSpeed = 0;
        float verticalSpeed = 0;

        if (Input.GetKey(left))
        {
            horizontalSpeed = -moveSpeed;
        }

        if (Input.GetKey(right))
        {
            horizontalSpeed = moveSpeed;
        }

        if (Input.GetKey(back))
        {
            verticalSpeed = -moveSpeed;
        }

        if (Input.GetKey(forward))
        {
            verticalSpeed = moveSpeed;
        }

        return new Vector3(horizontalSpeed, 0, verticalSpeed);
    }

    //wishDir: the direction the player wishes to go in the newest frame
    //wishSpeed: the speed the player wishes to go this frame
    private void ApplyGroundAcceleration(Vector3 wishDir, float wishSpeed, float surfaceFriction)
    {
        var currentSpeed = Vector3.Dot(newVelocity, wishDir); //Vector projection of the current velocity onto the new direction
        var speedToAdd = wishSpeed - currentSpeed;

        var acceleration = PlayerConstants.GroundAcceleration * Time.deltaTime; //acceleration to apply in the newest direction

        if (speedToAdd <= 0)
        {
            return;
        }

        var accelspeed = Mathf.Min(acceleration * wishSpeed * surfaceFriction, speedToAdd);
        newVelocity += accelspeed * wishDir; //add acceleration in the new direction
    }

    //wishDir: the direction the player  wishes to goin the newest frame
    //wishSpeed: the speed the player wishes to go this frame
    private void ApplyAirAcceleration(Vector3 wishDir, float wishSpeed)
    {
        var wishSpd = Mathf.Min(wishSpeed, PlayerConstants.AirAccelerationCap);
        var currentSpeed = Vector3.Dot(newVelocity, wishDir);
        var speedToAdd = wishSpd - currentSpeed;

        if (speedToAdd <= 0)
        {
            return;
        }

        var accelspeed = Mathf.Min(speedToAdd, PlayerConstants.AirAcceleration * wishSpeed * Time.deltaTime);
        newVelocity += accelspeed * wishDir;
    }

    private void ApplyFriction()
    {
        var speed = newVelocity.magnitude;

        //Don't apply friction if the player isn't moving
        //Clear speed if it's too low to prevent accidental movement
        if (speed < 0.01f)
        {
            newVelocity = Vector3.zero;
            return;
        }

        var lossInSpeed = speed * PlayerConstants.Friction * Time.deltaTime;
        var newSpeed = Mathf.Max(speed - lossInSpeed, 0);

        if (newSpeed != speed)
        {
            newVelocity *= newSpeed / speed; //Scale velocity based on friction
        }
    }

    private void ClampVelocity(float range)
    {
        newVelocity = Vector3.ClampMagnitude(newVelocity, PlayerConstants.MaxVelocity);
    }

    //Slide off of the impacting surface
    private void ClipVelocity(Vector3 normal)
    {
        // Determine how far along plane to slide based on incoming direction.
        var backoff = Vector3.Dot(newVelocity, normal);

        for (int i = 0; i < 3; i++)
        {
            var change = normal[i] * backoff;
            newVelocity[i] -= change;
        }

        // iterate once to make sure we aren't still moving through the plane
        var adjust = Vector3.Dot(newVelocity, normal);
        if (adjust < 0.0f)
        {
            newVelocity -= (normal * adjust);
        }
    }

    private void ResolveCollisions()
    {
        var center = transform.position + myCollider.center; // get center of bounding box in world space

        Vector3 extents = PlayerConstants.BoxCastExtents;
        if (crouching)
        {
            extents = PlayerConstants.CrouchingBoxCastExtents;
        }

        var overlaps = Physics.OverlapBox(center, extents, Quaternion.identity);

        foreach (var other in overlaps)
        {
            // If the collider is my own, check the next one
            if (other == myCollider || other.isTrigger)
            {
                continue;
            }

            if (Physics.ComputePenetration(myCollider, transform.position, transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 dir, out float dist))
            {
                if (Vector3.Dot(dir, newVelocity.normalized) > 0)
                {
                    continue;
                }

                Vector3 depenetrationVector = dir * dist; // The vector needed to get outside of the collision

                Debug.Log("depen: " + depenetrationVector.ToString("F5") + " proj " + Vector3.Project(newVelocity, -dir).ToString("F5"));

                if (!surfing)
                {
                    transform.position += depenetrationVector;
                    newVelocity -= Vector3.Project(newVelocity, -dir);
                }
                else
                {
                    ClipVelocity(dir);
                }
            }
        }
    }

}