using LoadBalancer.Game;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehaviour : MonoBehaviour
{
    private StarterAssetsInputs _inputs;
    private bool configured;
    private double lastSnapshotTime;
    public IClientPlayer Player { get; set; }

    private void OnEnable()
    {
        _inputs = GetComponent<StarterAssetsInputs>();
    }

    private void LateUpdate()
    {
        if (Player == null) return;
        if (configured) return;

        transform.position = Player.Properties.Position.ToVector();
        transform.rotation = Player.Properties.Rotation.ToQuaternion();

        Player.Properties.EnableChangeTracking();
        lastSnapshotTime = Time.realtimeSinceStartupAsDouble;

        GetComponent<PlayerInput>().enabled = true;
        GetComponent<BasicRigidBodyPush>().canPush = true;
        GetComponent<ThirdPersonController>().isFirstPerson = true;

        configured = true;
    }

    private double lastSpawnBoxTime;

    private void Update()
    {
        if (Player == null) return;
        if (!configured) return;

        if (NetClient.Instance.RoomObjectsEnabled
            && Keyboard.current.insertKey.wasPressedThisFrame && Time.realtimeSinceStartupAsDouble - lastSpawnBoxTime >= 1f / 2)
        {
            var position = transform.position + transform.forward * 2 + transform.up * 3;
            var prefab = NetClient.Instance.GetRoomObjectPrefab("Box");
            Instantiate(prefab, position, Quaternion.Euler(Vector3.zero));
            NetClient.Instance.LogInformation($"Cube spanwed at {position}");
            lastSpawnBoxTime = Time.realtimeSinceStartupAsDouble;
        }

        if (!NetClient.Instance.RoomObjectsEnabled
            && Keyboard.current.insertKey.wasPressedThisFrame && Time.realtimeSinceStartupAsDouble - lastSpawnBoxTime >= 1f / 2)
        {
            var position = transform.position + transform.forward * 2 + transform.up * 3;
            var prefab = NetClient.Instance.GetRoomObjectPrefab("Box");
            var gameObject = Instantiate(prefab, position, Quaternion.Euler(Vector3.zero));

            var rigidBody = gameObject.GetComponent<Rigidbody>();
            rigidBody.velocity = new Vector3(1, 0, 1);
            rigidBody.angularVelocity = new Vector3(1, 0, 1);
            rigidBody.isKinematic = false;

            lastSpawnBoxTime = Time.realtimeSinceStartupAsDouble;
        }
    }

    void FixedUpdate()
    {
        if (Player == null) return;
        if (!configured) return;

        Player.Properties.Position = transform.position.ToPoint();
        Player.Properties.Rotation = transform.rotation.ToPointEulerAngles();
        Player.Properties.MoveDirection = _inputs.move.ToPoint();
        Player.Properties.IsJump = _inputs.jump;
        Player.Properties.IsSprint = _inputs.sprint;

        if (Player.Properties.HasChangeAny(PlayerKeys.IsJump, PlayerKeys.IsSprint, PlayerKeys.MoveDirection) 
            || Time.realtimeSinceStartupAsDouble - lastSnapshotTime >= 1f / NetClient.Instance.frequency)
        {
            if (Player.CommitChanges())
                lastSnapshotTime = Time.realtimeSinceStartupAsDouble;
        }
    }
}
