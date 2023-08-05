using GameCarrier.Async;
using LoadBalancer.Common;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Utilities
{
    public static Vector2 ToVector(this Point2f point) => new Vector2(point.X, point.Y);
    public static Point2f ToPoint(this Vector2 vector) => new Point2f(vector.x, vector.y);

    public static Vector3 ToVector(this Point3f point) => new Vector3(point.X, point.Y, point.Z);
    public static Point3f ToPoint(this Vector3 vector) => new Point3f(vector.x, vector.y, vector.z);

    public static Quaternion ToQuaternion(this Point3f point) => Quaternion.Euler(point.ToVector() * 360);
    public static Point3f ToPointEulerAngles(this Quaternion quaternion) => (quaternion.eulerAngles / 360).ToPoint();

    public static GameObject FindChildWithTag(this GameObject parent, string tag)
    {
        GameObject child = null;

        foreach (Transform transform in parent.transform)
        {
            if (transform.CompareTag(tag))
            {
                child = transform.gameObject;
                break;
            }
        }

        return child;
    }

    private const float Epsilon = 0.001f;

    public static Vector3 Lerp(this Vector3 current, Vector3 target, float deltaTime)
    {
        if (deltaTime > 1) deltaTime = 1;
        var delta = target - current;
        if (delta.magnitude > Epsilon)
            return current + delta * deltaTime;
        else
            return target;
    }
    
    public static Quaternion Lerp(this Quaternion current, Quaternion target, float deltaTime)
    {
        if (deltaTime > 1) deltaTime = 1;
        var delta = target.eulerAngles - current.eulerAngles;
        if (delta.magnitude > Epsilon)
            return Quaternion.Euler(
                Mathf.LerpAngle(current.eulerAngles.x, target.eulerAngles.x, deltaTime),
                Mathf.LerpAngle(current.eulerAngles.y, target.eulerAngles.y, deltaTime),
                Mathf.LerpAngle(current.eulerAngles.z, target.eulerAngles.z, deltaTime));
        else
            return target;
    }

    public static InterpolationMode Maximum(this InterpolationMode mode, InterpolationMode max = InterpolationMode.Prediction) =>
        mode > max ? max : mode;

    public static Task LoadSceneAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        return new AsyncMessage()
            .Named(out var operation)
            .AddSubscription(new AsyncEventSubscription<Action<AsyncOperation>>(
                h => op.completed += h, h => op.completed -= h)
                .Handler(_ => operation.Complete()))
            .ExecuteAsync();
    }

    public static string GetDropDownValue(this GameObject gameObject, string zero = null)
    {
        var dropDown = gameObject.GetComponent<Dropdown>();

        if (zero != null && dropDown.value == 0)
            return zero;

        return dropDown.options[dropDown.value].text;
    }

    public static void SetDropDownValue(this GameObject gameObject, string text)
    {
        var dropDown = gameObject.GetComponent<Dropdown>();
        var index = dropDown.options.FindIndex(o => o.text == text);
        dropDown.value = index >= 0 ? index : 0;
    }
}