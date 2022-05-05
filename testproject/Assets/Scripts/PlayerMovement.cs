using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField]
    private float m_Speed = 20.0f;
    [SerializeField]
    private float m_RotSpeed = 5.0f;

    public static Dictionary<ulong, PlayerMovement> Players = new Dictionary<ulong, PlayerMovement>();

    private void Start()
    {
        if (IsLocalPlayer)
        {
            var temp = transform.position;
            temp.y = 0.5f;
            transform.position = temp;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Players[OwnerClientId] = this; // todo should really have a NetworkStop for unregistering this...
    }

    private void FixedUpdate()
    {
        if (IsLocalPlayer)
        {
            float speed = 0f;
            float rot = 0f;

            if (Application.isEditor)
            {
                speed = Input.GetAxis("Vertical");
                rot = Input.GetAxis("Horizontal");
            }
            else
            {
                if (!Input.gyro.enabled)
                {
                    Input.gyro.enabled = true;
                    Debug.LogWarning("*** GYRO SENSOR ENABLED ***");
                }
                if (Input.gyro != null && Input.gyro.enabled)
                {
                    const float damper = 33f;
                    speed = Input.gyro.rotationRate.x * damper;
                    rot =  Input.gyro.rotationRate.y ;
                }
            }

            transform.position += speed * 0.1f * m_Speed * Time.fixedDeltaTime * transform.forward;
            transform.rotation = Quaternion.Euler(0, rot * 90f * m_RotSpeed * Time.fixedDeltaTime, 0) * transform.rotation;
        }
    }
}
