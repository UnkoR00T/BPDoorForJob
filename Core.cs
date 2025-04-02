using BrokeProtocol.API;
using BrokeProtocol.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BrokeProtocol.Managers;
using UnityEngine;
using UnityEngine.Windows;

namespace DoorForJobV3
{
    public class Core : Plugin
    {
        public Core() {
            Info = new PluginInfo("DoorForJobV3", "dfjv3");
        }
    }
    public class Door : MonoBehaviour
    {
        public bool Open { get; set; } = false;
        public bool Locked { get; set; } = false;
        public bool Animating { get; set; } = false;
        public int Deg { get; set; }
        public string Job { get; set; } = String.Empty;
        public int Time { get; set; }
        public string Axis { get; set; } = "y";
        public float x { get; set; } = 0;
        public float y { get; set; } = 0;
        public float z { get; set; } = 0;
    }
    public class Event : EntityEvents
    {
        [Execution(ExecutionMode.Event)]
        public override bool Initialize(ShEntity e)
        {


            if (!String.IsNullOrWhiteSpace(e.data))
            {
                string pattern = @"^time=""(\d+)"",deg=""(-?\d+)""(?:,rotation_axis=""([^""]+)"")?(?:,x=""([^""]+)"")?(?:,y=""([^""]+)"")?(?:,z=""([^""]+)"")?(?:,job=""([^""]+)"")?(?:,open_label=""([^""]+)"")?(?:,lock_label=""([^""]+)"")?$";
                Match match = Regex.Match(e.data, pattern);

                if (match.Success)
                {
                    string time = match.Groups[1].Value;
                    string deg = match.Groups[2].Value;
                    string axis = match.Groups[3].Success ? match.Groups[3].Value : "y";
                    string x = match.Groups[4].Success ? match.Groups[4].Value : "0";
                    string y = match.Groups[5].Success ? match.Groups[5].Value : "0";
                    string z = match.Groups[6].Success ? match.Groups[6].Value : "0";
                    string job = match.Groups[7].Success ? match.Groups[7].Value : String.Empty;
                    string openLabel = match.Groups[8].Success ? match.Groups[8].Value : "Open/Close";
                    string lockLabel = match.Groups[9].Success ? match.Groups[9].Value : "Lock/Unlock";


                    Door door = e.gameObject.AddComponent<Door>();
                    door.Deg = int.Parse(deg);
                    door.Job = job; 
                    door.Time = int.Parse(time);
                    door.Axis = axis;
                    door.x = float.Parse(x);
                    door.y = float.Parse(y);
                    door.z = float.Parse(z);
                    e.svEntity.SvAddDynamicAction("openDoor", openLabel);
                    if (!String.IsNullOrWhiteSpace(door.Job))
                    {
                        e.svEntity.SvAddDynamicAction("lockDoor", lockLabel);
                    }
                }
            }


            return true;
        }
        [CustomTarget]
        public void lockDoor(ShEntity target, ShPlayer player)
        {
            if (target.gameObject.TryGetComponent<Door>(out Door door))
            {
                if (String.IsNullOrWhiteSpace(door.Job)) return;
                if (player.svPlayer.job.info.shared.jobName != door.Job)
                {
                    player.svPlayer.SendGameMessage($"&c[DOOR] You dont have required job! ({door.Job})");
                    return;
                }
                door.Locked = !door.Locked;
                if (door.Locked)
                {
                    player.svPlayer.SendGameMessage("&c[DOOR] Door has been locked.");
                }
                else
                {
                    player.svPlayer.SendGameMessage("&c[DOOR] Door has been unlocked.");
                }
            }
        }
        [CustomTarget]
        public void openDoor(ShEntity target, ShPlayer player)
        {
            if (target.gameObject.TryGetComponent<Door>(out Door door))
            {
                if (!String.IsNullOrWhiteSpace(door.Job))
                {
                    if (door.Locked)
                    {
                        player.svPlayer.SendGameMessage("&c[DOOR] These doors are locked.");
                        return;
                    }
                }

                if (door.Animating)
                {
                    player.svPlayer.SendGameMessage("&c[DOOR] These doors are animating right now.");
                    return;
                }
                int degChange = door.Open ? -door.Deg : door.Deg;
                float x_change = door.Open ? -door.x : door.x;
                float y_change = door.Open ? -door.y : door.y;
                float z_change = door.Open ? -door.z : door.z;
                Vector3 pos = target.transform.position;
                pos.x += x_change;
                pos.y += y_change;
                pos.z += z_change;
                door.Animating = true;
                target.StartCoroutine(AnimateMovementAndRotation(target, pos, degChange, door.Time, door.Axis));
                door.Open = !door.Open;
            }
        }
        public IEnumerator AnimateMovementAndRotation(ShEntity entity, Vector3 targetPosition, float targetRotation, float duration, string axis = "y")
        {
            if (entity.TryGetComponent<Door>(out Door door))
            {
                Vector3 startPosition = entity.transform.position;
                Vector3 startRotation = entity.transform.rotation.eulerAngles;
                Vector3 targetRotationVector = startRotation;
                switch (axis.ToLower())
                {
                    case "x":
                        targetRotationVector.x += targetRotation;
                        break;
                    case "y":
                        targetRotationVector.y += targetRotation;
                        break;
                    case "z":
                        targetRotationVector.z += targetRotation;
                        break;
                    default:
                        targetRotationVector.y = targetRotation;
                        break;
                }

                float timeElapsed = 0f;
                var transform = entity.transform;

                while (timeElapsed < duration)
                {
                    timeElapsed += Time.deltaTime;
                    Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, timeElapsed / duration);
                    Vector3 currentRotation = Vector3.Lerp(startRotation, targetRotationVector, timeElapsed / duration);
                    transform.position = currentPosition;
                    transform.rotation = Quaternion.Euler(currentRotation);
                    entity.svEntity.SvRelocate(transform);

                    yield return null;
                }
                transform.position = targetPosition;
                transform.rotation = Quaternion.Euler(targetRotationVector);
                entity.svEntity.SvRelocate(transform);
                door.Animating = false;
            }
        }



    }
}
