using System;

namespace MultiplayerInfrastructure.ItemSystem
{
  [Serializable]
  public struct TypedActionResult<T>
  {
    public ActionResult Result;
    public T Data;

    public TypedActionResult(ActionResult result, T data)
    {
      Result = result;
      Data = data;
    }
  }
}
