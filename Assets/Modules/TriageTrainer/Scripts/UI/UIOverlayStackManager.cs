using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.UI
{
  public class UIOverlayStackManager : MonoBehaviour
  {
    private static UIOverlayStackManager _instance;

    public static UIOverlayStackManager Instance
    {
      get
      {
        if (_instance == null)
        {
          GameObject go = new GameObject("UIOverlayStackManager");
          DontDestroyOnLoad(go);
          _instance = go.AddComponent<UIOverlayStackManager>();
        }
        return _instance;
      }
    }
    
    private readonly Stack<IUIOverlay> _stack = new ();

    public void Push(IUIOverlay overlay)
    {
      if (overlay == null) return;

      if (_stack.Count > 0)
      {
        if (_stack.Peek() == overlay)
        {
          return;
        }

        _stack.Peek().OnOverlayPopped();
      }

      _stack.Push(overlay);
      overlay.OnOverlayPushed();
    }

    public void Pop(IUIOverlay overlay)
    {
      if (!IsTop(overlay)) return;

      _stack.Pop();
      overlay.OnOverlayPopped();

      if (_stack.Count > 0)
      {
        _stack.Peek().OnOverlayPushed();
      }
    }
    
    public bool IsTop(IUIOverlay overlay) => _stack.Count > 0 && _stack.Peek() == overlay;
  }
}
