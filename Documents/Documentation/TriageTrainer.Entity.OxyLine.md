# <a id="TriageTrainer_Entity_OxyLine"></a> Namespace TriageTrainer.Entity.OxyLine

### Classes

 [OxyLineConnectionPoint](TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint.md)

Marks the position where an oxygen line can be connected.
Connection behavior will be implemented separately.

 [OxyLinePairInteractable](TriageTrainer.Entity.OxyLine.OxyLinePairInteractable.md)

Exposes one oxygen-line connection action from either endpoint of a configured pair.
Both instances must reference each other. The action is unavailable until the required
patient display object is active and the associated wall flowmeter is attached.

