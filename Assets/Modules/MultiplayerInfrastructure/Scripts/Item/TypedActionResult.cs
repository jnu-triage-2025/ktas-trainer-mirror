using System;

namespace MultiplayerInfrastructure.Item
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
