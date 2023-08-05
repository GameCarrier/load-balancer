using UnityEngine;
using UnityEngine.InputSystem;

public class BasicRigidBodyPush : MonoBehaviour
{
	public LayerMask pushLayers;
	public bool canPush;
	[Range(0.5f, 5f)] public float strength = 1.1f;

	private GameObject lastHitGameObject;
	private double lastHitTime;

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		var component = hit.gameObject.GetComponent<RoomObjectBehaviour>();
		if (component != null && component.enabled)
		{
			lastHitGameObject = hit.gameObject;
			lastHitTime = Time.realtimeSinceStartupAsDouble;
		}

		if (canPush) PushRigidBodies(hit);
	}

    private void Update()
    {
		if (Keyboard.current.deleteKey.wasPressedThisFrame && lastHitGameObject != null && Time.realtimeSinceStartupAsDouble - lastHitTime < 5f)
		{
			var component = lastHitGameObject.GetComponent<RoomObjectBehaviour>();
			if (component != null && component.enabled)
			{
				Destroy(lastHitGameObject);
				lastHitGameObject = null;
				return;
			}
		}
	}

    private void PushRigidBodies(ControllerColliderHit hit)
	{
		// https://docs.unity3d.com/ScriptReference/CharacterController.OnControllerColliderHit.html

		// make sure we hit a non kinematic rigidbody
		Rigidbody body = hit.collider.attachedRigidbody;
		if (body == null) return;

		// make sure we only push desired layer(s)
		var bodyLayerMask = 1 << body.gameObject.layer;
		if ((bodyLayerMask & pushLayers.value) == 0) return;

		// We dont want to push objects below us
		if (hit.moveDirection.y < -0.3f) return;

		// Calculate push direction from move direction, horizontal motion only
		Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);

		// Apply the push and take strength into account
		if (!body.isKinematic)
		{
			body.AddForce(pushDir * strength, ForceMode.Impulse);
			Debug.Log($"{System.DateTime.Now:mm:ss.fff} Push {pushDir * strength}");
		}
		else
		{
			var component = hit.gameObject.GetComponent<RoomObjectBehaviour>();
			if (component != null && component.enabled)
			{
				component.ApplyForcePassive(pushDir * strength);
				if (!body.isKinematic)
				{
					body.AddForce(pushDir * strength, ForceMode.Impulse);
					Debug.Log($"{System.DateTime.Now:mm:ss.fff} Finally Push {pushDir * strength}");
				}
			}
		}
	}
}