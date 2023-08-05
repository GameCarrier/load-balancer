using UnityEngine;

public class HotReloader : MonoBehaviour
{
    public string str;
    private static GameObject instance;
    private static GameObject nonExisting;

    public static bool IsHotReload
    {
        get
        {
            if (instance == null)
            {
                if (nonExisting == null)
                    nonExisting = new GameObject();

                instance = GameObject.Find("HotReloader");
                if (instance == null)
                    instance = nonExisting;
            }

            if (ReferenceEquals(nonExisting, instance))
                return false;

            return instance.GetComponent<HotReloader>().str == string.Empty;
        }
    }
    public void Awake()
    {
        str = "_";
    }

    public void OnDisable()
    {
        str = null;
    }
}
