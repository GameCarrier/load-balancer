using LoadBalancer.Common;
using LoadBalancer.Game;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehaviour3rd : MonoBehaviour
{
    private StarterAssetsInputs _inputs;
    private bool configured;
    private TransformInterpolator Interpolator;
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

        Player.OnPropertiesChanged += ApplyPropertyChanges;
        Player.OnLeave += Player_OnLeave;

        Interpolator = new TransformInterpolator(transform);
        Interpolator.SetFrequency(NetClient.Instance.frequency);
        Interpolator.SetMode(NetClient.Instance.InterpolationMode);

        GetComponent<PlayerInput>().enabled = false;
        GetComponent<BasicRigidBodyPush>().canPush = false;
        GetComponent<ThirdPersonController>().isFirstPerson = false;

        configured = true;
    }

    private void ApplyPropertyChanges(KeyValueCollection changes)
    {
        Interpolator.SetMode(NetClient.Instance.InterpolationMode);
        Interpolator.SetFrequency(NetClient.Instance.frequency);
        Interpolator.SetTargetPosition(Player.Properties.Position.ToVector());
        Interpolator.SetTargetRotation(Player.Properties.Rotation.ToQuaternion());

        //transform.position = Player.Properties.Position.ToVector();
        //transform.rotation = Player.Properties.Rotation.ToQuaternion();
        _inputs.move = Player.Properties.MoveDirection.ToVector();
        _inputs.sprint = Player.Properties.IsSprint;
        _inputs.jump = Player.Properties.IsJump;
    }

    private void Update()
    {
        if (Player == null) return;
        if (!configured) return;
        
        Interpolator.InterpolatePosition();
        Interpolator.InterpolateRotation();
    }

    private void Player_OnLeave() => Destroy(gameObject);
}
