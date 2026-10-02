using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS.Gameplay
{
    public class RocketProjectile : Throwable
    {
        protected override void OnClientLaunch(Vector3 position, Quaternion rotation, Vector3 force)
        {
            transform.SetPositionAndRotation(position, rotation);
            _rigidBody.isKinematic = false;
            _rigidBody.AddForce(force);
        }

        protected override void OnClientDetonate(Vector3 detonationPosition)
        {
            base.OnClientDetonate(detonationPosition);
            _rigidBody.isKinematic = true;
        }
    }
}