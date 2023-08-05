using LoadBalancer.Common;
using LoadBalancer.Game;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public enum RoomObjectMode
{ 
    None,
    ActiveOnHost,
    ActiveOnOwner,
    DynamicHost,
}

public class RoomObjectBehaviour : MonoBehaviour
{
    public RoomObjectMode Mode = RoomObjectMode.DynamicHost;

    protected bool isRegistered { get; private set; }
    protected bool isConfiguring { get; private set; }
    public bool isConfigured { get; private set; }

    protected Rigidbody _rigidbody;
    protected Collider _collider;

    protected bool IsHost => NetClient.Instance.IsConnected && NetClient.Instance.CurrentPlayer.Properties.IsHost;

    public IClientRoomObject RoomObject { get; set; }

    private void Awake()
    {
        // enabled = NetClient.Instance.RoomObjectsEnabled;
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        Interpolator = new TransformInterpolator(transform);
    }

    #region Configure

    private async void LateUpdate()
    {
        if (NetClient.Instance == null) return;
        if (!NetClient.Instance.RoomObjectsEnabled) return;
        if (Mode == RoomObjectMode.None) return;

        if (!isRegistered)
        {
            NetClient.Instance.SceneRoomObjects.Add(this);
            isRegistered = true;
        }

        if (isConfiguring) return;
        if (isConfigured) return;
        if (!NetClient.Instance.IsConnected) return;

        if (RoomObject == null)
        {
            isConfiguring = true;

            try
            {
                if (_rigidbody != null) _rigidbody.isKinematic = true;
                if (_rigidbody != null) _rigidbody.useGravity = false;
                if (_collider != null) _collider.enabled = false;
                
                await Task.Delay(10);
                
                RoomObject = await NetClient.Instance.DetectRoomObject(this);
            }
            finally
            {
                if (RoomObject == null)
                    Destroy(gameObject);
                else
                {
                    if (_rigidbody != null) _rigidbody.useGravity = true;
                    if (_collider != null) _collider.enabled = true;

                    isConfiguring = false;
                    isConfigured = true;
                }
            }
        }

        if (RoomObject != null && !NetClient.Instance.IsConnected)
            RoomObject = null;

        if (RoomObject != null)
        {
            RoomObject.OnDestroy += RoomObject_OnDestroy;
            RoomObject.OnPropertiesChanged += ApplyHostChange;
            ConfigureMode();
        }

        isConfiguring = false;
        isConfigured = true;
    }

    private void ApplyHostChange(KeyValueCollection changes)
    {
        switch (Mode)
        {
            case RoomObjectMode.ActiveOnHost:
            case RoomObjectMode.DynamicHost:
                if (changes.ContainsKey(RoomObjectKeys.HostId))
                    ConfigureMode();
                break;
        }
    }

    public void ConfigureMode()
    {
        Interpolator.SetFrequency(NetClient.Instance.frequency);
        Interpolator.SetMode(NetClient.Instance.InterpolationMode.Maximum(InterpolationMode.Prediction));

        switch (Mode)
        {
            case RoomObjectMode.ActiveOnHost:
                TryConfigureAsPassive(!IsHost);
                TryConfigureAsActive(IsHost);
                break;

            case RoomObjectMode.ActiveOnOwner:
                bool activeOwner = RoomObject.Properties.OwnerId == NetClient.Instance.CurrentPlayer.PlayerId;
                TryConfigureAsPassive(!activeOwner);
                TryConfigureAsActive(activeOwner);
                break;

            case RoomObjectMode.DynamicHost:
                bool activeHost = RoomObject.Properties.HostId == NetClient.Instance.CurrentPlayer.PlayerId;
                TryConfigureAsPassive(!activeHost);
                TryConfigureAsActive(activeHost);
                break;
        }
    }

    public void ClearState()
    {
        isRegistered = false;
        isConfiguring = false;
        isConfigured = false;
        configuredAsActive = false;
        configuredAsPassive = false;
        ResetRoomObject();
    }

    private void ResetRoomObject()
    {
        if (RoomObject != null)
        {
            RoomObject.OnDestroy -= RoomObject_OnDestroy;
            RoomObject.OnPropertiesChanged -= ApplyHostChange;
            RoomObject.OnPropertiesChanged -= ApplyPropertyChanges;
        }
        RoomObject = null;
    }

    #endregion

    #region Destroy

    private void RoomObject_OnDestroy()
    {
        ResetRoomObject();
        Destroy(gameObject);
    }

    private async void OnDestroy()
    {
        if (NetClient.Instance == null) return;
        if (!NetClient.Instance.RoomObjectsEnabled) return;
        if (Mode == RoomObjectMode.None) return;

        NetClient.Instance.SceneRoomObjects.Remove(this);

        if (!NetClient.Instance.IsConnected) return;

        if (RoomObject == null) return;

        await Task.Delay(1);    // don't remove - this should let NetClient's OnDestroy go first, if we stop application

        if (NetClient.Instance == null) return;

        if (!NetClient.Instance.IsConnected) return;

        RoomObject.Destroy();
    }

    #endregion

    #region Forces

    private void OnCollisionStay(Collision collision)
    {
        if (NetClient.Instance == null) return;
        if (!NetClient.Instance.RoomObjectsEnabled) return;
        if (RoomObject == null) return;
        if (Mode == RoomObjectMode.None) return;
        if (!configuredAsActive) return;
        if (!enabledAsActive) return;

        if (Mode != RoomObjectMode.DynamicHost) return;
        if (RoomObject.Properties.HostId != NetClient.Instance.CurrentPlayer.PlayerId) return;

        var component = collision.gameObject.GetComponent<RoomObjectBehaviour>();
        if (component != null && component.enabled && component.Mode == RoomObjectMode.DynamicHost
            && Time.realtimeSinceStartupAsDouble - component.lastChangeTime >= 0.1f)
        {
            if (component.RoomObject.Properties.HostId != NetClient.Instance.CurrentPlayer.PlayerId)
            {
                component.RoomObject.EnableChangeTracking();
                component.RoomObject.Properties.HostId = NetClient.Instance.CurrentPlayer.PlayerId;
                component.ConfigureMode();
            }
        }
    }

    private const float ApplyForceInterval = 0.09f;
    private const float AccumulateForceInterval = 0.045f;
    private int MaxForceHistoryTimes = 3;

    class ForceRecord
    {
        public Vector3 Force;
        public double Interval;
        public int Times;
    }

    private double lastSendForceTime;
    private double lastAddForceTime;

    private readonly List<ForceRecord> forceHistory = new List<ForceRecord>();
    private readonly List<ForceRecord> forceInput = new List<ForceRecord>();

    public void ApplyForcePassive(Vector3 force)
    {
        if (NetClient.Instance == null) return;
        if (!NetClient.Instance.RoomObjectsEnabled) return;
        if (RoomObject == null) return;
        if (Mode == RoomObjectMode.None) return;
        if (!configuredAsPassive) return;
        if (!NetClient.Instance.IsConnected) return;

        switch (Mode)
        {
            case RoomObjectMode.ActiveOnHost:
                if (IsHost) return;
                break;
            
            case RoomObjectMode.ActiveOnOwner:
                if (RoomObject.Properties.OwnerId == NetClient.Instance.CurrentPlayer.PlayerId)
                    return;
                break;
            
            case RoomObjectMode.DynamicHost:
                if (RoomObject.Properties.HostId == NetClient.Instance.CurrentPlayer.PlayerId)
                    return;

                if (Time.realtimeSinceStartupAsDouble - lastChangeTime >= 0.1f)
                {
                    RoomObject.EnableChangeTracking();
                    RoomObject.Properties.HostId = NetClient.Instance.CurrentPlayer.PlayerId;
                    ConfigureMode();
                    return;
                }
                break;
        }

        // send force to Host
        if (Time.realtimeSinceStartupAsDouble - lastSendForceTime >= ApplyForceInterval)
        {
            forceHistory.Add(new ForceRecord { Force = force, Interval = Time.realtimeSinceStartupAsDouble });
            Debug.Log($"{System.DateTime.Now:mm:ss.fff} Add force {force}");
        }
    }

    public void ApplyForceActive(RoomEventArguments args)
    {
        if (NetClient.Instance == null) return;
        if (!NetClient.Instance.RoomObjectsEnabled) return;
        if (RoomObject == null) return;
        if (Mode == RoomObjectMode.None) return;
        if (!configuredAsActive) return;
        
        switch (Mode)
        {
            case RoomObjectMode.ActiveOnHost:
                if (!IsHost) return;
                break;

            case RoomObjectMode.ActiveOnOwner:
                if (RoomObject.Properties.OwnerId != NetClient.Instance.CurrentPlayer.PlayerId)
                    return;
                break;

            case RoomObjectMode.DynamicHost:
                if (RoomObject.Properties.HostId != NetClient.Instance.CurrentPlayer.PlayerId)
                    return;
                break;
        }

        // apply force on Host
        var input = new ForceRecord { Force = args.Force.ToVector(), Interval = args.Interval, Times = args.Times };
        forceInput.Clear();
        forceInput.Add(input);

        Debug.Log($"{System.DateTime.Now:mm:ss.fff} Receive force {input.Force} over {input.Interval} times {input.Times}");
    }

    private void UpdateForces()
    {
        if (NetClient.Instance == null) return;
        if (!NetClient.Instance.RoomObjectsEnabled) return;
        if (RoomObject == null) return;
        if (Mode == RoomObjectMode.None) return;

        // send
        if (forceHistory.Any() 
            && (forceHistory.Count >= MaxForceHistoryTimes || Time.realtimeSinceStartupAsDouble - forceHistory[0].Interval >= AccumulateForceInterval))
        {
            Vector3 force = Vector3.zero;
            double interval = 0;
            for (int i = 0; i < forceHistory.Count; i++)
            {
                force += forceHistory[i].Force;
                if (i > 0)
                    interval += forceHistory[i].Interval - forceHistory[i - 1].Interval;
            }

            force /= forceHistory.Count;
            if (forceHistory.Count > 1)
                interval /= forceHistory.Count - 1;

            string recipientId = null;
            switch (Mode)
            {
                case RoomObjectMode.ActiveOnHost:
                    recipientId = NetClient.Instance.HostId;
                    break;
                case RoomObjectMode.ActiveOnOwner:
                    recipientId = RoomObject.Properties.OwnerId;
                    break;
                case RoomObjectMode.DynamicHost:
                    recipientId = RoomObject.Properties.HostId;
                    break;
            }

            if (recipientId != null && NetClient.Instance.CurrentRoom.Players[recipientId] != null)
            {
                interval = MaxForceHistoryTimes == 3 ? 0.015f : 0.08f;
                NetClient.Instance.CurrentRoom.RaiseRoomEvent(GameMethods.ApplyForce, new RoomEventArguments
                {
                    ObjectId = RoomObject.ObjectId,
                    Force = force.ToPoint(),
                    Interval = (float)interval,
                    Times = forceHistory.Count,
                }, recipientId);

                Debug.Log($"{System.DateTime.Now:mm:ss.fff} Send force {force} over {interval} times {forceHistory.Count}");
            }

            forceHistory.Clear();
            lastSendForceTime = Time.realtimeSinceStartupAsDouble;

            MaxForceHistoryTimes = MaxForceHistoryTimes == 3 ? 5 : 3;
        }

        // receive
        if (forceInput.Any() && Time.realtimeSinceStartupAsDouble - lastAddForceTime >= forceInput[0].Interval)
        {
            var input = forceInput[0];

            if (_rigidbody != null && !_rigidbody.isKinematic)
                _rigidbody.AddForce(input.Force, ForceMode.Impulse);

            Debug.Log($"{System.DateTime.Now:mm:ss.fff} Apply force {input.Force} over {input.Interval} times {input.Times}");

            input.Times--;
            if (input.Times <= 0) forceInput.RemoveAt(0);
            lastAddForceTime = Time.realtimeSinceStartupAsDouble;
        }
    }

    #endregion

    #region Active

    private bool configuredAsActive;
    private bool enabledAsActive;
    private double lastSnapshotTime;

    private void TryConfigureAsActive(bool enable)
    {
        // unload
        if (!NetClient.Instance.IsConnected || !enable && configuredAsActive)
        {
            RoomObject.Properties.DisableChangeTracking();
            configuredAsActive = false;
        }

        // init
        if (NetClient.Instance.IsConnected && enable && !configuredAsActive)
        {
            RoomObject.Properties.EnableChangeTracking();
            lastSnapshotTime = Time.realtimeSinceStartupAsDouble;
            if (_rigidbody != null) _rigidbody.isKinematic = false;
            configuredAsActive = true;
            Debug.Log($"{name} configured as ACTIVE");
        }

        enabledAsActive = enable;
    }

    private void FixedUpdate()
    {
        if (NetClient.Instance == null) return;
        if (!NetClient.Instance.RoomObjectsEnabled) return;
        if (RoomObject == null) return;
        if (Mode == RoomObjectMode.None) return;
        if (!configuredAsActive) return;
        if (!enabledAsActive) return;

        GetProperties(RoomObject.Properties);

        if (Time.realtimeSinceStartupAsDouble - lastSnapshotTime >= 1f / NetClient.Instance.frequency)
        {
            if (RoomObject.CommitChanges())
            {
                lastSnapshotTime = Time.realtimeSinceStartupAsDouble;
                Debug.Log($"Send position {RoomObject.Properties.Name}: {RoomObject.Properties.Position}");
            }
        }
    }

    #endregion

    #region Passive

    private bool configuredAsPassive;
    private bool enabledAsPassive;
    private TransformInterpolator Interpolator;
    private double lastChangeTime;

    private void TryConfigureAsPassive(bool enable)
    {
        // unload
        if (!NetClient.Instance.IsConnected || !enable && configuredAsPassive)
        {
            RoomObject.OnPropertiesChanged -= ApplyPropertyChanges;
            configuredAsPassive = false;
        }

        // init
        if (NetClient.Instance.IsConnected && enable && !configuredAsPassive)
        {
            Interpolator.Reset();
            RoomObject.OnPropertiesChanged += ApplyPropertyChanges;
            if (_rigidbody != null) _rigidbody.isKinematic = true;
            configuredAsPassive = true;
            Debug.Log($"{name} configured as PASSIVE");
        }

        enabledAsPassive = enable;
    }

    private void ApplyPropertyChanges(KeyValueCollection changes)
    {
        SetProperties(RoomObject, immediate: false);
        lastChangeTime = Time.realtimeSinceStartupAsDouble;
    }

    private void Update()
    {
        if (NetClient.Instance == null) return;
        if (!NetClient.Instance.RoomObjectsEnabled) return;
        if (Mode == RoomObjectMode.None) return;

        UpdateForces();

        if (RoomObject == null) return;
        if (!configuredAsPassive) return;
        if (!enabledAsPassive) return;

        Interpolator.InterpolatePosition();
        Interpolator.InterpolateRotation();
    }

    #endregion

    #region Virtual methods

    public virtual void GetProperties(RoomObjectProperties properties)
    {
        properties.Name = gameObject.name;
        properties.Position = transform.position.ToPoint();
        properties.Rotation = transform.rotation.ToPointEulerAngles();

        if (_rigidbody != null && NetClient.Instance.RoomObjectsSyncVelocity)
        {
            properties.Velocity = _rigidbody.velocity.ToPoint();
            properties.AngularVelocity = _rigidbody.angularVelocity.ToPoint();
        }
    }

    public virtual void SetProperties(IClientRoomObject obj, bool immediate)
    {
        gameObject.name = obj.Properties.Name;
        Interpolator.SetMode(NetClient.Instance.InterpolationMode.Maximum(InterpolationMode.Prediction));
        Interpolator.SetFrequency(NetClient.Instance.frequency);
        Interpolator.SetTargetPosition(obj.Properties.Position.ToVector(), immediate);
        Interpolator.SetTargetRotation(obj.Properties.Rotation.ToQuaternion(), immediate);

        Debug.Log($"Receive position {obj.Properties.Name}: {obj.Properties.Position}");

        if (_rigidbody != null && NetClient.Instance.RoomObjectsSyncVelocity)
        {
            _rigidbody.velocity = obj.Properties.Velocity.ToVector();
            _rigidbody.angularVelocity = obj.Properties.AngularVelocity.ToVector();
        }
    }

    #endregion
}
