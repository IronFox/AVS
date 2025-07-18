﻿using AVS.BaseVehicle;
using AVS.Engines;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace AVS.VehicleComponents
{
    public class AutoPilotNavigator : MonoBehaviour
    {
        public void Update()
        {
            if (GetComponent<AvsVehicle>() != null && GetComponent<AvsVehicle>().GetPilotingMode())
            {
                StopAllCoroutines();
                return;
            }
        }
        public void NaiveGo(Vector3 destination)
        {
            UWE.CoroutineHost.StartCoroutine(GoStraightToDestination(destination));
        }

        public void FaceDestinationFrame(Vector3 dest)
        {
            Vector3 direction = (dest - transform.position).normalized;
            Quaternion goal = Quaternion.LookRotation(direction);
            float rotationSpeed = 1f;
            while (0.01f < Quaternion.Angle(transform.rotation, goal))
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, goal, rotationSpeed * Time.deltaTime);
            }
        }
        public IEnumerator GoStraightToDestination(Vector3 dest)
        {
            AbstractEngine engine = GetComponent<AbstractEngine>();
            if (engine == null)
            {
                yield break;
            }
            IEnumerator ForwardLoop(float power)
            {
                float now = Time.time;
                while (Time.time < now + 0.1f)
                {
                    engine.ApplyPlayerControls(Vector3.forward * power);
                    engine.ExecutePhysicsMove();
                    yield return new WaitForFixedUpdate();
                }
            }
            IEnumerator BreakLoop()
            {
                float now = Time.time;
                while (Time.time < now + 0.1f && 4 < gameObject.GetComponent<Rigidbody>().velocity.magnitude)
                {
                    engine.ApplyPlayerControls(-Vector3.forward);
                    engine.ExecutePhysicsMove();
                    yield return new WaitForFixedUpdate();
                }
            }
            bool RaycastForward(float distance)
            {
                RaycastHit[] allHits = Physics.RaycastAll(transform.position, transform.forward, distance);
                var but = allHits
                    .Where(hit => hit.transform.GetComponent<Creature>() == null) // ignore creatures
                    .Where(hit => hit.transform.GetComponent<Player>() == null) // ignore player
                    ;
                return 0 < but.Count();
            }
            bool CheckClose(Vector3 destin, float magnit)
            {
                return (magnit < Vector3.Distance(transform.position, destin)) && RaycastForward(magnit);
            }

            while (20 < Vector3.Distance(transform.position, dest))
            {
                yield return new WaitForFixedUpdate();
                FaceDestinationFrame(dest);
                if (CheckClose(dest, 5f))
                {
                    Logger.PDANote(Language.main.Get("VFAutopilotHint1"));
                    break;
                }
                if (CheckClose(dest, 25f))
                {
                    yield return UWE.CoroutineHost.StartCoroutine(BreakLoop());
                    if (gameObject.GetComponent<Rigidbody>().velocity.magnitude < 0.1f)
                    {
                        Logger.PDANote(Language.main.Get("VFAutopilotHint1"));
                        yield break;
                    }
                }
                else if (CheckClose(dest, 50f))
                {
                    yield return UWE.CoroutineHost.StartCoroutine(ForwardLoop(0.25f));
                }
                else if (CheckClose(dest, 75f))
                {
                    yield return UWE.CoroutineHost.StartCoroutine(ForwardLoop(0.5f));
                }
                else
                {
                    yield return UWE.CoroutineHost.StartCoroutine(ForwardLoop(1f));
                }
            }
        }
    }
}
