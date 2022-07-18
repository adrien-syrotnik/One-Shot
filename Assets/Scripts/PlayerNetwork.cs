using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerNetwork : NetworkBehaviour
{
    private readonly NetworkVariable<PlayerNetworkData> _playerData = new(writePerm: NetworkVariableWritePermission.Owner);
    private Vector3 _vel;
    private float _rotVel;

    void Start()
    {
        
    }

    void Update()
    {
        if(IsOwner)
        {
            _playerData.Value = new PlayerNetworkData()
            {
                Position = transform.position,
                Rotation = transform.rotation.eulerAngles
            };
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, _playerData.Value.Position, ref _vel, 0.1f);
            transform.rotation = Quaternion.Euler(
                0,
                Mathf.SmoothDampAngle(transform.rotation.eulerAngles.y, _playerData.Value.Rotation.y, ref _rotVel, 0.1f),
                0);
        }
    }

    struct PlayerNetworkData : INetworkSerializable
    {
        private float _x, _y, _z;
        private float _yRot;

        internal Vector3 Position
        {
            get { return new Vector3(_x, _y, _z); }
            set
            {
                _x = value.x;
                _y = value.y;
                _z = value.z;
            }
        }

        internal Vector3 Rotation
        {
            get { return new Vector3(0, _yRot, 0); }
            set { _yRot = value.y; }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _x);
            serializer.SerializeValue(ref _y);
            serializer.SerializeValue(ref _z);
            serializer.SerializeValue(ref _yRot);
        }
    }
}
