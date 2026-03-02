// Definitions
// 컴파일 타임에 이미 정의되어있어야 함.
// 인스턴스화 시 이들 데이터를 근거로 아이템이 인스턴스화됨

// Definitions/Commons
string Identifier
string DisplayName
string Description
string DetailComment
string Color

// Definitions/Stack
bool IsStackable
int MaxStackCount

// Definitions/Durability
bool HasDurability
bool EnabledDeltaDurability
int MaxDurability
int DeltaDurabilityOnAttack
int DeltaDurabilityOnUse

// Definitions/ItemUsing
float MinReach
float MaxReach
int ItemDamage
bool EnabledCooldown
float CooldownMilliseconds

// Definitions/Instantiate
bool IsModifiedCurrentSerializedDerivedAttributes -> flag // 최적화를 위해서, 만약 NBT가 수정되었으면 겹치지 않게 하기 위해

// Handlers
OnGet
OnThrow
OnInteract
OnAttack
OnUse

// Internal
PlayAnimationOnAttack
PlayAnimationOnUse -> 그냥 Y축 방향으로 위아래 잠깐 왔다갔다하는걸로 통일 가능한지.. 아니다 그냥 띄워도 될듯

// Internal/Virtual
SetCurrentSerializedDerivedAttributes -> // abstract
GetCurrentSerializedDerivedAttributes -> // abstract 


// Instance
// Instance/Commons
string CurrentIdentifier
string CurrentDisplayName
string CurrentDescription
string CurrentDetailComment
Color CurrentColor
Sprite CurrentItemIconTexture // default: getting using Identifier

// Instance/Stack
bool IsCurrentlyStackable
int CurrentMaxStackCount
int CurrentStackCount

// Instance/Durability
bool HasCurrentDurability
bool CurrentEnabledDeltaDurability
int CurrentMaxDurability
int CurrentDurability
int CurrentDurabilityDeltaOnAttack
int CurrentDurabilityDeltaOnUse

// Instance/ItemUsing
float CurrentMinReach
float CurrentMaxReach
int CurrentItemDamage
bool CurrentEnabledCooldown
float CurrentCooldownMilliseconds
float CurrentCooldownRemainingMilliseconds

// Instance/Inheritance
string CurrentSerializedDerivedAttributes
