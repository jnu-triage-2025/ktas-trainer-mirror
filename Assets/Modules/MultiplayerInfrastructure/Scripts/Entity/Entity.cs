namespace MultiplayerInfrastructure.Entity
{
  public class Entity
  {
    private int _health = 100;

    public void Attack(Entity target, int damageAmount = 0)
    {
      if (target == null)
        return;
      target.OnAttacked(this, damageAmount);
    }

    public void OnAttacked(Entity attacker, int damageAmount)
    {
      _health -= damageAmount;
    }
  }
}
