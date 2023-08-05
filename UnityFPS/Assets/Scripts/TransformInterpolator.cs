using UnityEngine;

public enum InterpolationMode
{
    None,
    Interpolation,
    Prediction,
}

public class TransformInterpolator
{
    private Transform transform;
    private float frequency = 5;

    public InterpolationMode Mode { get; set; } = InterpolationMode.Interpolation;

    public TransformInterpolator(Transform transform) => this.transform = transform;

    private float deltaTime = 0;
    private double currTime = 0;

    private Vector3 nextPosition;
    private Vector3 lastPosition;
    private bool lerpPosition;

    private Quaternion lastRotation;
    private bool lerpRotation;

    public bool IsActive => lerpPosition || lerpRotation;

    public void SetFrequency(float frequency)
    {
        this.frequency = frequency;
    }

    public void SetMode(InterpolationMode mode)
    {
        Mode = mode;
    }

    public void SetTargetPosition(Vector3 target, bool immediate = false)
    {
        double time = Time.realtimeSinceStartupAsDouble;
        if (currTime > 0)
            deltaTime = (float)(time - currTime);
        currTime = time;

        var mode = immediate ? InterpolationMode.None : Mode;
        switch (mode)
        {
            case InterpolationMode.None:
                transform.position = target;
                lerpPosition = false;
                break;

            case InterpolationMode.Interpolation:
                lastPosition = target;
                lerpPosition = transform.position != lastPosition;
                break;

            case InterpolationMode.Prediction:

                var delta = deltaTime > 0
                    ? new Vector3(target.x - lastPosition.x, (target.y - lastPosition.y) / 4, target.z - lastPosition.z)
                    : Vector3.zero;
                delta /= 2;

                lastPosition = target;

                nextPosition = target + delta;
                lerpPosition = transform.position != nextPosition;
                break;
        }
    }

    public void SetTargetRotation(Quaternion target, bool immediate = false)
    {
        var mode = immediate ? InterpolationMode.None : Mode;
        switch (mode)
        {
            case InterpolationMode.None:
                transform.rotation = target;
                lerpRotation = false;
                break;

            case InterpolationMode.Interpolation:
            case InterpolationMode.Prediction:
                lastRotation = target;
                lerpRotation = transform.rotation != lastRotation;
                break;
        }
    }

    public void InterpolatePosition()
    {
        switch (Mode)
        {
            case InterpolationMode.Interpolation:
                if (!lerpPosition) return;
                transform.position = transform.position.Lerp(lastPosition, Time.deltaTime * frequency * 2);
                lerpPosition = transform.position != lastPosition;
                break;

            case InterpolationMode.Prediction:
                if (!lerpPosition) return;
                if (deltaTime == 0) return;
                transform.position = transform.position.Lerp(nextPosition, Time.deltaTime * frequency * 2);
                lerpPosition = transform.position != nextPosition;
                if (!lerpPosition)
                {
                    nextPosition = lastPosition;
                    lerpPosition = transform.position != nextPosition;
                }
                break;
        }
    }

    public void InterpolateRotation()
    {
        switch (Mode)
        {
            case InterpolationMode.Interpolation:
            case InterpolationMode.Prediction:
                if (!lerpRotation) return;
                transform.rotation = transform.rotation.Lerp(lastRotation, Time.deltaTime * frequency * 2);
                lerpRotation = transform.rotation != lastRotation;
                break;
        }
    }

    public void Reset()
    {
        deltaTime = 0;
        currTime = 0;
        lerpPosition = false;
        lerpRotation = false;
    }
}
