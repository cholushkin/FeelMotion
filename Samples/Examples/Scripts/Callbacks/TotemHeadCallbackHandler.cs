using UnityEngine;

public class TotemHeadCallbackHandler : MonoBehaviour
{
    public void OnPlay()
    {
        Debug.Log("OnPlay");
        
    }
    
    public void OnComplete()
    {
        Debug.Log("OnComplete");
    }
    
    
    public void OnStop()
    {
        Debug.Log("OnStop");
    }
}
