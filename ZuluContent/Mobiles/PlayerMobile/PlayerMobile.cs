using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Server.Misc;
using Server.Items;
using Server.Gumps;
using Server.Multis;
using Server.Engines.Help;
using Server.Network;
using Server.Regions;
using Server.Accounting;
using Scripts.Zulu.Engines.Classes;
using Scripts.Zulu.Engines.CustomSpellHotBar;
using Scripts.Zulu.Packets;
using Scripts.Zulu.Engines.Races;
using Server.ContextMenus;
using static Scripts.Zulu.Engines.Classes.SkillCheck;
using Server.Engines.Magic;
using Server.Engines.PartySystem;
using Server.Spells;
using ZuluContent.Zulu.Engines.Magic.Enums;
using ZuluContent.Zulu.Items;
using CalcMoves = Server.Movement.Movement;
using ZuluContent.Zulu.Engines.Magic;

namespace Server.Mobiles
{
    public partial class PlayerMobile : Mobile, IZuluClassed, IShilCheckSkill, IEnchanted, IBuffable,
        IElementalResistible, IZuluRace
    {
        private class CountAndTimeStamp
        {
            private int m_Count;

            public CountAndTimeStamp()
            {
            }

            public DateTime TimeStamp { get; private set; }

            public int Count
            {
                get { return m_Count; }
                set
                {
                    m_Count = value;
                    TimeStamp = DateTime.Now;
                }
            }
        }

        private bool m_IgnoreMobiles; // IgnoreMobiles should be moved to Server.Mobiles

        private Guilds.RankDefinition m_GuildRank;

        private List<Mobile> m_AllFollowers;

        #region Getters & Setters

        public List<Mobile> RecentlyReported { get; set; }

        public List<Mobile> AllFollowers
        {
            get
            {
                if (m_AllFollowers == null)
                    m_AllFollowers = new List<Mobile>();
                return m_AllFollowers;
            }
        }

        public Guilds.RankDefinition GuildRank
        {
            get
            {
                if (AccessLevel >= AccessLevel.GameMaster)
                    return Guilds.RankDefinition.Leader;
                else
                    return m_GuildRank;
            }
            set { m_GuildRank = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GuildMessageHue { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AllianceMessageHue { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Profession { get; set; }

        public int StepsTaken { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsStealthing => AllowedStealthSteps > 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IgnoreMobiles // IgnoreMobiles should be moved to Server.Mobiles
        {
            get { return m_IgnoreMobiles; }
            set
            {
                if (m_IgnoreMobiles != value)
                {
                    m_IgnoreMobiles = value;
                    Delta(MobileDelta.Flags);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public NpcGuild NpcGuild { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NpcGuildJoinTime { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastOnline { get; set; }
        
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastPowerHourUsed { get; set; }
        
        [CommandProperty(AccessLevel.GameMaster)]
        public BaseMount InternalizedMount { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public long LastMoved
        {
            get { return LastMoveTime; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NpcGuildGameTime { get; set; }

        #endregion

        #region PlayerFlags

        public PlayerFlag Flags { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PagingSquelched
        {
            get { return GetFlag(PlayerFlag.PagingSquelched); }
            set { SetFlag(PlayerFlag.PagingSquelched, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool KarmaLocked
        {
            get { return GetFlag(PlayerFlag.KarmaLocked); }
            set { SetFlag(PlayerFlag.KarmaLocked, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UseOwnFilter
        {
            get { return GetFlag(PlayerFlag.UseOwnFilter); }
            set { SetFlag(PlayerFlag.UseOwnFilter, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PublicMyRunUO
        {
            get { return GetFlag(PlayerFlag.PublicMyRunUO); }
            set
            {
                SetFlag(PlayerFlag.PublicMyRunUO, value);
                InvalidateMyRunUO();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AcceptGuildInvites
        {
            get { return GetFlag(PlayerFlag.AcceptGuildInvites); }
            set { SetFlag(PlayerFlag.AcceptGuildInvites, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RefuseTrades
        {
            get { return GetFlag(PlayerFlag.RefuseTrades); }
            set { SetFlag(PlayerFlag.RefuseTrades, value); }
        }

        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime AnkhNextUse { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan DisguiseTimeLeft
        {
            get { return DisguiseTimers.TimeRemaining(this); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsInPowerHour => (Core.Now - LastPowerHourUsed).TotalHours <= 1.0;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime PeacedUntil { get; set; }

        public QuestArrow QuestArrow
        {
            get => m_QuestArrow;
            set
            {
                if (m_QuestArrow != value)
                {
                    m_QuestArrow?.Stop();

                    m_QuestArrow = value;
                }
            }
        }

        public void ClearQuestArrow() => m_QuestArrow = null;

        public static Direction GetDirection4(Point3D from, Point3D to)
        {
            int dx = from.X - to.X;
            int dy = from.Y - to.Y;

            int rx = dx - dy;
            int ry = dx + dy;

            Direction ret;

            if (rx >= 0 && ry >= 0)
                ret = Direction.West;
            else if (rx >= 0 && ry < 0)
                ret = Direction.South;
            else if (rx < 0 && ry < 0)
                ret = Direction.East;
            else
                ret = Direction.North;

            return ret;
        }

        public override bool OnDroppedItemToWorld(Item item, Point3D location)
        {
            if (!base.OnDroppedItemToWorld(item, location))
                return false;

            BounceInfo bi = item.GetBounce();

            if (bi != null)
            {
                Type type = item.GetType();

                if (type.IsDefined(typeof(FurnitureAttribute), true) ||
                    type.IsDefined(typeof(DynamicFlipingAttribute), true))
                {
                    object[] objs = type.GetCustomAttributes(typeof(FlipableAttribute), true);

                    if (objs != null && objs.Length > 0)
                    {
                        FlipableAttribute fp = objs[0] as FlipableAttribute;

                        if (fp != null)
                        {
                            int[] itemIDs = fp.ItemIDs;

                            Point3D oldWorldLoc = bi.WorldLoc;
                            Point3D newWorldLoc = location;

                            if (oldWorldLoc.X != newWorldLoc.X || oldWorldLoc.Y != newWorldLoc.Y)
                            {
                                Direction dir = GetDirection4(oldWorldLoc, newWorldLoc);

                                if (itemIDs.Length == 2)
                                {
                                    switch (dir)
                                    {
                                        case Direction.North:
                                        case Direction.South:
                                            item.ItemID = itemIDs[0];
                                            break;
                                        case Direction.East:
                                        case Direction.West:
                                            item.ItemID = itemIDs[1];
                                            break;
                                    }
                                }
                                else if (itemIDs.Length == 4)
                                {
                                    switch (dir)
                                    {
                                        case Direction.South:
                                            item.ItemID = itemIDs[0];
                                            break;
                                        case Direction.East:
                                            item.ItemID = itemIDs[1];
                                            break;
                                        case Direction.North:
                                            item.ItemID = itemIDs[2];
                                            break;
                                        case Direction.West:
                                            item.ItemID = itemIDs[3];
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        public override int GetPacketFlags(bool stygianAbyss)
        {
            int flags = base.GetPacketFlags(stygianAbyss);

            if (m_IgnoreMobiles)
                flags |= 0x10;

            return flags;
        }

        public bool GetFlag(PlayerFlag flag)
        {
            return (Flags & flag) != 0;
        }

        public void SetFlag(PlayerFlag flag, bool value)
        {
            if (value)
                Flags |= flag;
            else
                Flags &= ~flag;
        }

        public static void Initialize()
        {
            EventSink.Login += OnLogin;
            EventSink.Logout += OnLogout;
            EventSink.Connected += EventSink_Connected;
            EventSink.Disconnected += EventSink_Disconnected;
        }

        private MountBlock m_MountBlock;

        public BlockMountType MountBlockReason
        {
            get { return CheckBlock(m_MountBlock) ? m_MountBlock.m_Type : BlockMountType.None; }
        }

        private static bool CheckBlock(MountBlock block)
        {
            return block != null && block.m_Timer.Running;
        }

        private class MountBlock
        {
            public BlockMountType m_Type;
            public Timer m_Timer;

            public MountBlock(TimeSpan duration, BlockMountType type, Mobile mobile)
            {
                m_Type = type;

                m_Timer = Timer.DelayCall(duration, () => RemoveBlock(mobile));
            }

            private void RemoveBlock(Mobile mobile)
            {
                (mobile as PlayerMobile).m_MountBlock = null;
            }
        }

        public void SetMountBlock(BlockMountType type, TimeSpan duration, bool dismount)
        {
            if (dismount)
            {
                if (Mount != null)
                {
                    Mount.Rider = null;
                }
            }

            if (m_MountBlock == null || !m_MountBlock.m_Timer.Running ||
                m_MountBlock.m_Timer.Next < DateTime.UtcNow + duration)
            {
                m_MountBlock = new MountBlock(duration, type, this);
            }
        }

        protected override void OnRaceChange(Race oldRace)
        {
            ValidateEquipment();
        }

        public override int MaxWeight
        {
            get { return 40 + (int) (3.5 * Str); }
        }

        private int m_LastGlobalLight = -1, m_LastPersonalLight = -1;

        public override void OnNetStateChanged()
        {
            m_LastGlobalLight = -1;
            m_LastPersonalLight = -1;
        }

        public override void CheckLightLevels(bool forceResend)
        {
            NetState ns = NetState;

            if (ns == null)
                return;

            int global, personal;

            ComputeLightLevels(out global, out personal);

            if (!forceResend)
                forceResend = global != m_LastGlobalLight || personal != m_LastPersonalLight;

            if (!forceResend)
                return;

            m_LastGlobalLight = global;
            m_LastPersonalLight = personal;

            ns.SendGlobalLightLevel(global);
            ns.SendPersonalLightLevel(Serial, personal);
        }

        private static void OnLogin(Mobile from)
        {
            if (from is PlayerMobile playerMobile && playerMobile.CustomSpellHotBars.Count > 0)
            {
                foreach (var hotBar in playerMobile.CustomSpellHotBars)
                {
                    from.SendGump(new CustomSpellHotBarGump(hotBar));
                }
            }
            
            if (AccountHandler.LockdownLevel <= AccessLevel.Player)
                return;

            string notice;
            Account acct = from.Account as Account;

            if (acct == null || !acct.HasAccess(from.NetState))
            {
                notice = from.AccessLevel == AccessLevel.Player
                    ? "The server is currently under lockdown. No players are allowed to log in at this time."
                    : "The server is currently under lockdown. You do not have sufficient access level to connect.";

                Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => Disconnect(from));
            }
            else if (from.AccessLevel >= AccessLevel.Administrator)
            {
                notice =
                    "The server is currently under lockdown. As you are an administrator, you may change this from the [Admin gump.";
            }
            else
            {
                notice = "The server is currently under lockdown. You have sufficient access level to connect.";
            }

            from.SendGump(new NoticeGump(1060637, 30720, notice, 0xFFC000, 300, 140, null));
        }

        private bool m_NoDeltaRecursion;

        public void ValidateEquipment()
        {
            if (m_NoDeltaRecursion || Map == null || Map == Map.Internal)
                return;

            if (Items == null)
                return;

            m_NoDeltaRecursion = true;
            Timer.DelayCall(TimeSpan.Zero, ValidateEquipment_Sandbox);
        }

        private void ValidateEquipment_Sandbox()
        {
            try
            {
                if (Map == null || Map == Map.Internal)
                    return;

                List<Item> items = Items;

                if (items == null)
                    return;

                bool moved = false;

                int str = Str;
                int dex = Dex;
                int intel = Int;

                Mobile from = this;

                for (int i = items.Count - 1; i >= 0; --i)
                {
                    if (i >= items.Count)
                        continue;

                    Item item = items[i];

                    if (item is BaseWeapon weapon)
                    {
                        bool drop = false;

                        if (dex < weapon.DexRequirement)
                            drop = true;
                        else if (str < weapon.StrRequirement)
                            drop = true;
                        else if (intel < weapon.IntRequirement)
                            drop = true;
                        else if (weapon.RequiredRace != null && weapon.RequiredRace != Race)
                            drop = true;

                        if (drop)
                        {
                            string name = weapon.Name;

                            if (name == null)
                                name = $"#{weapon.LabelNumber}";

                            from.SendLocalizedMessage(1062001, name); // You can no longer wield your ~1_WEAPON~
                            from.AddToBackpack(weapon);
                            moved = true;
                        }
                    }
                    else if (item is BaseArmor armor)
                    {
                        bool drop = false;

                        if (!armor.AllowMaleWearer && !from.Female && from.AccessLevel < AccessLevel.GameMaster)
                        {
                            drop = true;
                        }
                        else if (!armor.AllowFemaleWearer && from.Female && from.AccessLevel < AccessLevel.GameMaster)
                        {
                            drop = true;
                        }
                        else if (armor.RequiredRace != null && armor.RequiredRace != Race)
                        {
                            drop = true;
                        }
                        else
                        {
                            int strBonus = armor.ComputeStatBonus(StatType.Str),
                                strReq = armor.ComputeStatReq(StatType.Str);
                            int dexBonus = armor.ComputeStatBonus(StatType.Dex),
                                dexReq = armor.ComputeStatReq(StatType.Dex);
                            int intBonus = armor.ComputeStatBonus(StatType.Int),
                                intReq = armor.ComputeStatReq(StatType.Int);

                            if (dex < dexReq || dex + dexBonus < 1)
                                drop = true;
                            else if (str < strReq || str + strBonus < 1)
                                drop = true;
                            else if (intel < intReq || intel + intBonus < 1)
                                drop = true;
                        }

                        if (drop)
                        {
                            string name = armor.Name;

                            if (name == null)
                                name = $"#{armor.LabelNumber}";

                            if (armor is BaseShield)
                                from.SendLocalizedMessage(1062003, name); // You can no longer equip your ~1_SHIELD~
                            else
                                from.SendLocalizedMessage(1062002, name); // You can no longer wear your ~1_ARMOR~

                            from.AddToBackpack(armor);
                            moved = true;
                        }
                    }
                    else if (item is BaseClothing clothing)
                    {
                        bool drop = false;

                        if (!clothing.AllowMaleWearer && !from.Female && from.AccessLevel < AccessLevel.GameMaster)
                        {
                            drop = true;
                        }
                        else if (!clothing.AllowFemaleWearer && from.Female &&
                                 from.AccessLevel < AccessLevel.GameMaster)
                        {
                            drop = true;
                        }
                        else if (clothing.RequiredRace != null && clothing.RequiredRace != Race)
                        {
                            drop = true;
                        }
                        else
                        {
                            int strBonus = clothing.ComputeStatBonus(StatType.Str);
                            int strReq = clothing.ComputeStatReq(StatType.Str);

                            if (str < strReq || str + strBonus < 1)
                                drop = true;
                        }

                        if (drop)
                        {
                            string name = clothing.Name;

                            if (name == null)
                                name = $"#{clothing.LabelNumber}";

                            from.SendLocalizedMessage(1062002, name); // You can no longer wear your ~1_ARMOR~

                            from.AddToBackpack(clothing);
                            moved = true;
                        }
                    }
                }

                if (moved)
                    from.SendLocalizedMessage(500647); // Some equipment has been moved to your backpack.
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                m_NoDeltaRecursion = false;
            }
        }

        public override void Delta(MobileDelta flag)
        {
            base.Delta(flag);

            if ((flag & MobileDelta.Stat) != 0)
                ValidateEquipment();

            if ((flag & (MobileDelta.Name | MobileDelta.Hue)) != 0)
                InvalidateMyRunUO();
        }

        private static void Disconnect(object state)
        {
            NetState ns = ((Mobile) state).NetState;

            ns?.Disconnect("Disconnected due to lockdown");
        }

        private static void OnLogout(Mobile mobile)
        {
        }

        private static void EventSink_Connected(Mobile mobile)
        {
            if (mobile is PlayerMobile pm)
            {
                pm.SessionStart = DateTime.Now;
                pm.BedrollLogout = false;
                pm.LastOnline = DateTime.Now;
            }

            DisguiseTimers.StartTimer(mobile);
        }

        private static void EventSink_Disconnected(Mobile from)
        {
            if (from is PlayerMobile pm)
            {
                pm.m_GameTime += DateTime.Now - pm.SessionStart;
                pm.SpeechLog = null;
                pm.LastOnline = DateTime.Now;
                pm.ClearQuestArrow();
            }

            DisguiseTimers.StopTimer(from);
        }

        public override void OnSubItemAdded(Item item)
        {
            if (AccessLevel < AccessLevel.GameMaster && item.IsChildOf(Backpack))
            {
                int maxWeight = WeightOverloading.GetMaxWeight(this);
                int curWeight = BodyWeight + TotalWeight;

                if (curWeight > maxWeight)
                    SendLocalizedMessage(1019035, true, $" : {curWeight} / {maxWeight}");
            }
        }

        public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
        {
            if (target is BaseCreature creature && creature.IsInvulnerable || target is PlayerVendor ||
                target is TownCrier)
            {
                if (message)
                {
                    if (target.Title == null)
                        SendMessage("{0} cannot be harmed.", target.Name);
                    else
                        SendMessage("{0} {1} cannot be harmed.", target.Name, target.Title);
                }

                return false;
            }

            return base.CanBeHarmful(target, message, ignoreOurBlessedness);
        }

        public override void OnItemAdded(Item item)
        {
            base.OnItemAdded(item);

            if (item is BaseArmor || item is BaseWeapon)
            {
                Hits = Hits;
                Stam = Stam;
                Mana = Mana;
            }

            if (NetState != null)
                CheckLightLevels(false);

            OutgoingZuluPackets.SendZuluPlayerStatus(NetState, this);

            InvalidateMyRunUO();
        }

        public override void OnItemRemoved(Item item)
        {
            base.OnItemRemoved(item);

            if (item is BaseArmor || item is BaseWeapon)
            {
                Hits = Hits;
                Stam = Stam;
                Mana = Mana;
            }

            if (NetState != null)
                CheckLightLevels(false);

            OutgoingZuluPackets.SendZuluPlayerStatus(NetState, this);

            InvalidateMyRunUO();
        }

        public override double ArmorRating
        {
            get
            {
                var mod = this.GetAllEnchantmentsOfType<IArmorMod>().Sum(b => b.ArmorMod);
                var rating = Items.OfType<IArmorRating>().Sum(i => i.ArmorRatingScaled);
                var value = VirtualArmor + VirtualArmorMod + rating + mod;

                return value >= 0 ? value : 0;
            }
        }
        
        public List<CustomSpellHotBar> CustomSpellHotBars { get; private set; } = new();

        #region [Zulu] Resistances

        public EnchantmentDictionary Enchantments { get; private set; } = new();

        #endregion


        #region [Stats]Max

        [CommandProperty(AccessLevel.GameMaster)]
        public override int HitsMax => RawStr + GetStatOffset(StatType.Str);

        #endregion

        #region Stat Getters/Setters

        [CommandProperty(AccessLevel.GameMaster)]
        public override int Str
        {
            get { return base.Str; }
            set { base.Str = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int Int
        {
            get { return base.Int; }
            set { base.Int = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int Dex
        {
            get { return base.Dex; }
            set { base.Dex = value; }
        }

        #endregion

        public override bool Move(Direction d)
        {
            NetState ns = NetState;

            if (ns != null)
            {
                if (HasGump<ResurrectGump>())
                {
                    if (Alive)
                    {
                        CloseGump<ResurrectGump>();
                    }
                    else
                    {
                        SendLocalizedMessage(500111); // You are frozen and cannot move.
                        return false;
                    }
                }
            }

            var speed = ComputeMovementSpeed(d);

            bool res;

            if (!Alive)
                Movement.MovementImpl.IgnoreMovableImpassables = true;

            res = base.Move(d);

            Movement.MovementImpl.IgnoreMovableImpassables = false;

            if (!res)
                return false;

            m_NextMovementTime += speed;

            return true;
        }

        private bool m_LastProtectedMessage;
        private int m_NextProtectionCheck = 10;

        public virtual void RecheckTownProtection()
        {
            m_NextProtectionCheck = 10;

            GuardedRegion reg = (GuardedRegion) Region.GetRegion(typeof(GuardedRegion));
            bool isProtected = reg != null && !reg.IsDisabled();

            if (isProtected != m_LastProtectedMessage)
            {
                if (isProtected)
                    SendLocalizedMessage(500112); // You are now under the protection of the town guards.
                else
                    SendLocalizedMessage(500113); // You have left the protection of the town guards.

                m_LastProtectedMessage = isProtected;
            }
        }

        public override void MoveToWorld(Point3D loc, Map map)
        {
            base.MoveToWorld(loc, map);

            RecheckTownProtection();
        }

        public override void SetLocation(Point3D loc, bool isTeleport)
        {
            if (!isTeleport && AccessLevel == AccessLevel.Player)
            {
                // moving, not teleporting
                int zDrop = Location.Z - loc.Z;

                if (zDrop > 20) // we fell more than one story
                    Hits -= zDrop / 20 * 10 - 5; // deal some damage; does not kill, disrupt, etc
            }

            base.SetLocation(loc, isTeleport);

            if (isTeleport || --m_NextProtectionCheck == 0)
                RecheckTownProtection();
        }
        
        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from != this)
            {
                if (Alive)
                {
                    var theirParty = from.Party as Party;
                    var ourParty = Party as Party;

                    if (theirParty == null && ourParty == null)
                    {
                        list.Add(new AddToPartyEntry(from, this));
                    }
                    else if (theirParty != null && theirParty.Leader == from)
                    {
                        if (ourParty == null)
                        {
                            list.Add(new AddToPartyEntry(from, this));
                        }
                        else if (ourParty == theirParty)
                        {
                            list.Add(new RemoveFromPartyEntry(from, this));
                        }
                    }
                }

                var curhouse = BaseHouse.FindHouseAt(this);

                if (curhouse != null && Alive && curhouse.IsFriend(from))
                {
                    list.Add(new EjectPlayerEntry(from, this));
                }
            }
        }

        public override void Paralyze(TimeSpan duration)
        {
            bool paralyze = true;

            this.FireHook(h => h.OnParalysis(this, ref duration, ref paralyze));

            if (paralyze)
                base.Paralyze(duration);
        }

        public override void Freeze(TimeSpan duration)
        {
            bool freeze = true;

            this.FireHook(h => h.OnParalysis(this, ref duration, ref freeze));

            if (freeze)
                base.Freeze(duration);
        }

        private delegate void ContextCallback();

        public override bool CheckEquip(Item item)
        {
            if (!base.CheckEquip(item))
                return false;

            if (AccessLevel < AccessLevel.GameMaster && item.Layer != Layer.Mount && HasTrade)
            {
                BounceInfo bounce = item.GetBounce();

                if (bounce != null)
                {
                    if (bounce.Parent is Item)
                    {
                        Item parent = (Item) bounce.Parent;

                        if (parent == Backpack || parent.IsChildOf(Backpack))
                            return true;
                    }
                    else if (bounce.Parent == this)
                    {
                        return true;
                    }
                }

                SendLocalizedMessage(
                    1004042); // You can only equip what you are already carrying while you have a trade pending.
                return false;
            }

            return true;
        }

        public override bool CheckTrade(Mobile to, Item item, SecureTradeContainer cont, bool message, bool checkItems,
            int plusItems, int plusWeight)
        {
            int msgNum = 0;

            if (cont == null)
            {
                if (to.Holding != null)
                    msgNum = 1062727; // You cannot trade with someone who is dragging something.
                else if (HasTrade)
                    msgNum = 1062781; // You are already trading with someone else!
                else if (to.HasTrade)
                    msgNum = 1062779; // That person is already involved in a trade
                else if (to is PlayerMobile player && player.RefuseTrades)
                    msgNum = 1154111; // ~1_NAME~ is refusing all trades.
            }

            if (msgNum == 0)
            {
                if (cont != null)
                {
                    plusItems += cont.TotalItems;
                    plusWeight += cont.TotalWeight;
                }

                if (Backpack == null ||
                    !Backpack.CheckHold(this, item, false, checkItems, plusItems, plusWeight))
                    msgNum = 1004040; // You would not be able to hold this if the trade failed.
                else if (to.Backpack == null ||
                         !to.Backpack.CheckHold(to, item, false, checkItems, plusItems, plusWeight))
                    msgNum = 1004039; // The recipient of this trade would not be able to carry this.
                else
                    msgNum = CheckContentForTrade(item);
            }

            if (msgNum != 0)
            {
                if (message)
                {
                    if (msgNum == 1154111)
                        SendLocalizedMessage(msgNum, to.Name);
                    else
                        SendLocalizedMessage(msgNum);
                }

                return false;
            }

            return true;
        }

        // TODO: Add config check here if context menus are actually wanted
        public override bool CheckContextMenuDisplay(IEntity target) => true;

        private static int CheckContentForTrade(Item item)
        {
            if (item is TrapableContainer && ((TrapableContainer) item).TrapType != TrapType.None)
                return 1004044; // You may not trade trapped items.

            if (item is Container)
            {
                foreach (Item subItem in item.Items)
                {
                    int msg = CheckContentForTrade(subItem);

                    if (msg != 0)
                        return msg;
                }
            }

            return 0;
        }

        public override bool CheckNonlocalDrop(Mobile from, Item item, Item target)
        {
            if (!base.CheckNonlocalDrop(from, item, target))
                return false;

            if (from.AccessLevel >= AccessLevel.GameMaster)
                return true;

            Container pack = Backpack;
            if (from == this && HasTrade && (target == pack || target.IsChildOf(pack)))
            {
                BounceInfo bounce = item.GetBounce();

                if (bounce != null && bounce.Parent is Item)
                {
                    Item parent = (Item) bounce.Parent;

                    if (parent == pack || parent.IsChildOf(pack))
                        return true;
                }

                SendLocalizedMessage(1004041); // You can't do that while you have a trade pending.
                return false;
            }

            return true;
        }
        
        public override int GetHurtSound()
        {
            if (Female)
                return 0x14B + Utility.Random(5);
            
            return 0x154 + Utility.Random(5);
        }

        protected override void OnLocationChange(Point3D oldLocation)
        {
            CheckLightLevels(false);
        }

        protected override bool OnMove(Direction d)
        {
            var canMove = base.OnMove(d);
            this.FireHook(h => h.OnMove(this, d, ref canMove));
            return canMove;
        }

        public override void OnHiddenChanged()
        {
            base.OnHiddenChanged();
            this.FireHook(h => h.OnHiddenChanged(this));
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m is BaseCreature creature && !creature.Controlled)
                return !Alive || !creature.Alive || Hidden && AccessLevel > AccessLevel.Player;

            return base.OnMoveOver(m);
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            if (amount > 0)
            {
                var c = BandageContext.GetContext(this);
                c?.Slip();
            }

            WeightOverloading.FatigueOnDamage(this, amount);

            if (Combatant == null)
                Combatant = from;

            base.OnDamage(amount, from, willKill);
        }

        public override void Resurrect()
        {
            bool wasAlive = Alive;

            base.Resurrect();

            if (Alive && !wasAlive)
            {
                Item deathRobe = new DeathRobe();

                if (!EquipItem(deathRobe))
                    deathRobe.Delete();
                
                this.FireHook(h => h.OnResurrect(this));
            }
        }

        public override bool OnBeforeDeath()
        {
            NetState state = NetState;

            state?.CancelAllTrades();

            DropHolding();

            /*SendMessage("YOU DIED!");
            return false;*/
            
            return base.OnBeforeDeath();
        }

        public override DeathMoveResult GetParentMoveResultFor(Item item)
        {
            DeathMoveResult res = base.GetParentMoveResultFor(item);

            if (res == DeathMoveResult.MoveToCorpse && item.Movable && Young)
                res = DeathMoveResult.MoveToBackpack;

            return res;
        }

        public override DeathMoveResult GetInventoryMoveResultFor(Item item)
        {
            DeathMoveResult res = base.GetInventoryMoveResultFor(item);

            if (res == DeathMoveResult.MoveToCorpse && item.Movable && Young)
                res = DeathMoveResult.MoveToBackpack;

            return res;
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            HueMod = -1;
            NameMod = null;

            DisguiseTimers.RemoveTimer(this);

            if (PermaFlags.Count > 0)
            {
                PermaFlags.Clear();

                if (c is Corpse corpse)
                    corpse.Criminal = true;

                if (SkillHandlers.Stealing.ClassicMode)
                    Criminal = true;
            }

            var resurrect = false;

            this.FireHook(h => h.OnDeath(this, ref resurrect));
            
            if (resurrect)
            {
                Resurrect();

                if (Poison != null)
                    CurePoison(this);

                Hits = HitsMax;
                Mana = ManaMax;
                Stam = StamMax;
                Warmode = false;
                Hidden = true;

                var pack = Backpack;

                pack.TryDropItems(this, false, c.Items.ToArray());

                c.Delete();
            }
        }

        private Hashtable m_AntiMacroTable;
        private TimeSpan m_GameTime;
        private TimeSpan m_ShortTermElapse;
        private TimeSpan m_LongTermElapse;
        
        public SkillName Learning { get; set; } = (SkillName) (-1);

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastEscortTime { get; set; }

        public PlayerMobile()
        {
            VisibilityList = new List<Mobile>();
            PermaFlags = new List<Mobile>();
            m_AntiMacroTable = new Hashtable();
            RecentlyReported = new List<Mobile>();

            m_GameTime = TimeSpan.Zero;
            m_ShortTermElapse = TimeSpan.FromHours(8.0);
            m_LongTermElapse = TimeSpan.FromHours(40.0);

            m_GuildRank = Guilds.RankDefinition.Lowest;

            InvalidateMyRunUO();
        }

        #region Poison

        public override ApplyPoisonResult ApplyPoison(Mobile from, Poison poison)
        {
            if (!Alive)
                return ApplyPoisonResult.Immune;

            var result = base.ApplyPoison(from, poison);

            if (from != null && result == ApplyPoisonResult.Poisoned && PoisonTimer is PoisonImpl.PoisonTimer timer)
                timer.From = from;

            return result;
        }

        public override void OnPoisonImmunity(Mobile from, Poison poison)
        {
            if (Young)
                SendLocalizedMessage(
                    502808); // You would have been poisoned, were you not new to the land of Britannia. Be careful in the future.
            else
                base.OnPoisonImmunity(from, poison);
        }

        public override bool CheckPoisonImmunity(Mobile from, Poison poison)
        {
            var immune = base.CheckPoisonImmunity(@from, poison);
            this.FireHook(h => h.OnCheckPoisonImmunity(@from, this, poison, ref immune));

            return immune;
        }

        #endregion

        public PlayerMobile(Serial s) : base(s)
        {
            VisibilityList = new List<Mobile>();
            m_AntiMacroTable = new Hashtable();
            InvalidateMyRunUO();
        }

        public List<Mobile> VisibilityList { get; }

        public List<Mobile> PermaFlags { get; private set; }

        public override bool IsHarmfulCriminal(Mobile target)
        {
            if (SkillHandlers.Stealing.ClassicMode && target is PlayerMobile &&
                ((PlayerMobile) target).PermaFlags.Count > 0)
            {
                int noto = Notoriety.Compute(this, target);

                if (noto == Notoriety.Innocent)
                    target.Delta(MobileDelta.Noto);

                return false;
            }

            if (target is BaseCreature && ((BaseCreature) target).InitialInnocent &&
                !((BaseCreature) target).Controlled)
                return false;

            return base.IsHarmfulCriminal(target);
        }

        public bool AntiMacroCheck(Skill skill, object obj)
        {
            if (obj == null || m_AntiMacroTable == null || AccessLevel != AccessLevel.Player)
                return true;

            Hashtable tbl = (Hashtable) m_AntiMacroTable[skill];
            if (tbl == null)
                m_AntiMacroTable[skill] = tbl = new Hashtable();

            CountAndTimeStamp count = (CountAndTimeStamp) tbl[obj];
            if (count != null)
            {
                if (count.TimeStamp + Misc.SkillCheck.AntiMacroExpire <= DateTime.Now)
                {
                    count.Count = 1;
                    return true;
                }
                else
                {
                    ++count.Count;
                    if (count.Count <= Misc.SkillCheck.Allowance)
                        return true;
                    else
                        return false;
                }
            }
            else
            {
                tbl[obj] = count = new CountAndTimeStamp();
                count.Count = 1;

                return true;
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 34:
                {
                    InternalizedMount = reader.ReadEntity<BaseMount>();
                    goto case 33;
                }
                case 33:
                {
                    LastPowerHourUsed = reader.ReadDateTime();
                    goto case 32;
                }
                case 32:
                {
                    var count = reader.ReadInt();

                    CustomSpellHotBars = new List<CustomSpellHotBar>(count);

                    for (int i = 0; i < count; ++i)
                        CustomSpellHotBars.Add(CustomSpellHotBar.Deserialize(reader));

                    goto case 31;
                }
                case 31:
                {
                    TargetZuluClass = (ZuluClassType) reader.ReadInt();
                    goto case 30;
                }
                case 30:
                {
                    ZuluRaceType = (ZuluRaceType) reader.ReadInt();
                    goto case 29;
                }
                case 29:
                {
                    Enchantments = EnchantmentDictionary.Deserialize(reader);
                    foreach (var buff in Enchantments.Values.Values.OfType<IBuff>())
                        BuffManager.TryAddBuff(buff);
                    goto case 28;
                }
                case 28:
                {
                    PeacedUntil = reader.ReadDateTime();

                    goto case 27;
                }
                case 27:
                {
                    AnkhNextUse = reader.ReadDateTime();

                    goto case 26;
                }
                case 26:
                case 25:
                case 24:
                case 23:
                case 22:
                case 21:
                case 20:
                {
                    AllianceMessageHue = reader.ReadEncodedInt();
                    GuildMessageHue = reader.ReadEncodedInt();

                    goto case 19;
                }
                case 19:
                {
                    int rank = reader.ReadEncodedInt();
                    int maxRank = Guilds.RankDefinition.Ranks.Length - 1;
                    if (rank > maxRank)
                        rank = maxRank;

                    m_GuildRank = Guilds.RankDefinition.Ranks[rank];
                    LastOnline = reader.ReadDateTime();
                    goto case 18;
                }
                case 18:
                case 17:
                case 16:
                {
                    Profession = reader.ReadEncodedInt();
                    goto case 15;
                }
                case 15:
                case 14:
                case 13:
                case 12:
                case 11:
                case 10:
                {
                    if (reader.ReadBool())
                    {
                        HairItemIdReal = reader.ReadInt();
                        HairHueReal = reader.ReadInt();
                        FacialHairItemIdReal = reader.ReadInt();
                        FacialHairHueReal = reader.ReadInt();
                    }

                    goto case 9;
                }
                case 9:
                case 8:
                {
                    NpcGuild = (NpcGuild) reader.ReadInt();
                    NpcGuildJoinTime = reader.ReadDateTime();
                    NpcGuildGameTime = reader.ReadTimeSpan();
                    goto case 7;
                }
                case 7:
                {
                    PermaFlags = reader.ReadEntityList<Mobile>();
                    goto case 6;
                }
                case 6:
                case 5:
                case 4:
                case 3:
                case 2:
                {
                    Flags = (PlayerFlag) reader.ReadInt();
                    goto case 1;
                }
                case 1:
                {
                    m_LongTermElapse = reader.ReadTimeSpan();
                    m_ShortTermElapse = reader.ReadTimeSpan();
                    m_GameTime = reader.ReadTimeSpan();
                    goto case 0;
                }
                case 0:
                {
                    break;
                }
            }

            if (RecentlyReported == null)
                RecentlyReported = new List<Mobile>();

            // Professions weren't verified on 1.0 RC0
            if (!CharacterCreation.VerifyProfession(Profession))
                Profession = 0;

            if (PermaFlags == null)
                PermaFlags = new List<Mobile>();

            if (m_GuildRank == null)
                m_GuildRank =
                    Guilds.RankDefinition
                        .Member; //Default to member if going from older version to new version (only time it should be null)

            if (LastOnline == DateTime.MinValue && Account != null)
                LastOnline = ((Account) Account).LastLogin;

            if (AccessLevel > AccessLevel.Player)
                m_IgnoreMobiles = true;

            List<Mobile> list = Stabled;

            for (int i = 0; i < list.Count; ++i)
            {
                BaseCreature bc = list[i] as BaseCreature;

                if (bc != null)
                {
                    bc.IsStabled = true;
                    bc.StabledBy = this;
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            //cleanup our anti-macro table
            foreach (Hashtable t in m_AntiMacroTable.Values)
            {
                ArrayList remove = new ArrayList();
                foreach (CountAndTimeStamp time in t.Values)
                {
                    if (time.TimeStamp + Misc.SkillCheck.AntiMacroExpire <= DateTime.Now)
                        remove.Add(time);
                }

                for (int i = 0; i < remove.Count; ++i)
                    t.Remove(remove[i]);
            }

            CheckKillDecay();

            base.Serialize(writer);

            writer.Write((int) 34); // version
            
            writer.Write(InternalizedMount);

            writer.Write(LastPowerHourUsed);
            
            writer.Write(CustomSpellHotBars.Count);

            for (int i = 0; i < CustomSpellHotBars.Count; ++i)
                CustomSpellHotBars[i].Serialize(writer);
            
            writer.Write((int) TargetZuluClass);

            writer.Write((int) ZuluRaceType);
            
            Enchantments.Serialize(writer);

            writer.Write((DateTime) PeacedUntil);
            writer.Write((DateTime) AnkhNextUse);

            writer.WriteEncodedInt(AllianceMessageHue);
            writer.WriteEncodedInt(GuildMessageHue);

            writer.WriteEncodedInt(m_GuildRank.Rank);
            writer.Write(LastOnline);

            writer.WriteEncodedInt(Profession);

            bool useMods = HairItemIdReal != -1 || FacialHairItemIdReal != -1;

            writer.Write(useMods);

            if (useMods)
            {
                writer.Write((int) HairItemIdReal);
                writer.Write((int) HairHueReal);
                writer.Write((int) FacialHairItemIdReal);
                writer.Write((int) FacialHairHueReal);
            }

            writer.Write((int) NpcGuild);
            writer.Write((DateTime) NpcGuildJoinTime);
            writer.Write((TimeSpan) NpcGuildGameTime);

            writer.Write(PermaFlags);

            writer.Write((int) Flags);

            writer.Write(m_LongTermElapse);
            writer.Write(m_ShortTermElapse);
            writer.Write(GameTime);
        }

        public void CheckKillDecay()
        {
            if (m_ShortTermElapse < GameTime)
            {
                m_ShortTermElapse += TimeSpan.FromHours(8);
                if (ShortTermMurders > 0)
                    --ShortTermMurders;
                OutgoingZuluPackets.SendZuluPlayerStatus(NetState, this);
            }

            if (m_LongTermElapse < GameTime)
            {
                m_LongTermElapse += TimeSpan.FromHours(40);
                if (Kills > 0)
                    --Kills;
                OutgoingZuluPackets.SendZuluPlayerStatus(NetState, this);
            }
        }

        public void ResetKillTime()
        {
            m_ShortTermElapse = GameTime + TimeSpan.FromHours(8);
            m_LongTermElapse = GameTime + TimeSpan.FromHours(40);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime SessionStart { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan GameTime
        {
            get
            {
                if (NetState != null)
                    return m_GameTime + (DateTime.Now - SessionStart);
                else
                    return m_GameTime;
            }
        }

        public override bool CanSee(Mobile m)
        {
            if (m is PlayerMobile && ((PlayerMobile) m).VisibilityList.Contains(this))
                return true;

            return base.CanSee(m);
        }

        public virtual void CheckedAnimate(int action, int frameCount, int repeatCount, bool forward, bool repeat,
            int delay)
        {
            if (!Mounted)
            {
                base.Animate(action, frameCount, repeatCount, forward, repeat, delay);
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            BaseHouse.HandleDeletion(this);

            DisguiseTimers.RemoveTimer(this);

            InternalizedMount?.Delete();
        }

        public bool BedrollLogout { get; set; }

        private QuestArrow m_QuestArrow;

        #region MyRunUO Invalidation

        public bool ChangedMyRunUO { get; set; }

        public void InvalidateMyRunUO()
        {
            if (!Deleted && !ChangedMyRunUO)
            {
                ChangedMyRunUO = true;
                // Engines.MyRunUO.MyRunUO.QueueMobileUpdate( this );
            }
        }

        public override void OnKillsChange(int oldValue)
        {
            InvalidateMyRunUO();
        }

        public override void OnGenderChanged(bool oldFemale)
        {
            InvalidateMyRunUO();
        }

        public override void OnGuildChange(Guilds.BaseGuild oldGuild)
        {
            InvalidateMyRunUO();
        }

        public override void OnGuildTitleChange(string oldTitle)
        {
            InvalidateMyRunUO();
        }

        public override void OnKarmaChange(int oldValue)
        {
            InvalidateMyRunUO();
        }

        public override void OnFameChange(int oldValue)
        {
            InvalidateMyRunUO();
        }

        public override void OnSkillChange(SkillName skill, double oldBase)
        {
            ZuluClass?.ComputeClass();

            InvalidateMyRunUO();
        }

        public override void OnAccessLevelChanged(AccessLevel oldLevel)
        {
            if (AccessLevel == AccessLevel.Player)
                IgnoreMobiles = false;
            else
                IgnoreMobiles = true;

            InvalidateMyRunUO();
        }

        public override void OnRawStatChange(StatType stat, int oldValue)
        {
            InvalidateMyRunUO();
        }

        #endregion

        #region Fastwalk Prevention

        private static bool FastwalkPrevention = true; // Is fastwalk prevention enabled?

        private static readonly int FastwalkThreshold = 400; // Fastwalk prevention will become active after 0.4 seconds

        private long m_NextMovementTime;
        private bool m_HasMoved;

        public virtual bool UsesFastwalkPrevention
        {
            get { return AccessLevel < AccessLevel.Counselor; }
        }

        public override int ComputeMovementSpeed(Direction dir, bool checkTurning = true)
        {
            if (checkTurning && (dir & Direction.Mask) != (Direction & Direction.Mask))
                return CalcMoves.RunMountDelay; // We are NOT actually moving (just a direction change)

            bool running = (dir & Direction.Running) != 0;

            bool onHorse = Mount != null;

            if (onHorse)
                return running ? CalcMoves.RunMountDelay : CalcMoves.WalkMountDelay;

            return running ? CalcMoves.RunFootDelay : CalcMoves.WalkFootDelay;
        }

        #endregion

        #region Hair and beard mods

        public void SetHairMods(int? hairId = null, int? hairHue = null, int? facialId = null, int? facialHue = null)
        {
            if (hairId.HasValue)
            {
                if (HairItemIdReal == -1)
                    HairItemIdReal = HairItemID;

                HairItemID = hairId.Value;
            }

            if (hairHue.HasValue)
            {
                if (HairHueReal == -1)
                    HairHueReal = HairHue;

                HairHue = hairHue.Value;
            }

            if (facialId.HasValue)
            {
                if (FacialHairItemIdReal == -1)
                    FacialHairItemIdReal = FacialHairItemID;

                FacialHairItemID = facialId.Value;
            }

            if (facialHue.HasValue)
            {
                if (FacialHairHueReal == -1)
                    FacialHairHueReal = FacialHairHue;

                FacialHairHue = facialHue.Value;
            }
        }

        public void RemoveHairMods()
        {
            if (HairItemIdReal > -1)
            {
                HairItemID = HairItemIdReal;
                HairItemIdReal = -1;
            }

            if (HairHueReal > -1)
            {
                HairHue = HairHueReal;
                HairHueReal = -1;
            }

            if (FacialHairItemIdReal > -1)
            {
                FacialHairItemID = FacialHairItemIdReal;
                FacialHairItemIdReal = -1;
            }

            if (FacialHairHueReal > -1)
            {
                FacialHairHue = FacialHairHueReal;
                FacialHairHueReal = -1;
            }
        }

        public int HairItemIdReal { get; private set; } = -1;
        public int HairHueReal { get; private set; } = -1;
        public int FacialHairItemIdReal { get; private set; } = -1;
        public int FacialHairHueReal { get; private set; } = -1;

        #endregion

        #region Young system

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Young
        {
            get { return GetFlag(PlayerFlag.Young); }
            set { SetFlag(PlayerFlag.Young, value); }
        }

        public override string ApplyNameSuffix(string suffix)
        {
            if (Young)
            {
                if (suffix.Length == 0)
                    suffix = "(Young)";
                else
                    suffix = String.Concat(suffix, " (Young)");
            }

            return base.ApplyNameSuffix(suffix);
        }

        public override TimeSpan GetLogoutDelay()
        {
            if (Young || BedrollLogout || TestCenter.Enabled)
                return TimeSpan.Zero;

            return base.GetLogoutDelay();
        }

        private DateTime m_LastYoungMessage = DateTime.MinValue;

        public bool CheckYoungProtection(Mobile from)
        {
            if (!Young)
                return false;

            if (Region is BaseRegion && !((BaseRegion) Region).YoungProtected)
                return false;

            if (from is BaseCreature && ((BaseCreature) from).IgnoreYoungProtection)
                return false;

            if (DateTime.Now - m_LastYoungMessage > TimeSpan.FromMinutes(1.0))
            {
                m_LastYoungMessage = DateTime.Now;
                SendLocalizedMessage(
                    1019067); // A monster looks at you menacingly but does not attack.  You would be under attack now if not for your status as a new citizen of Britannia.
            }

            return true;
        }

        private DateTime m_LastYoungHeal = DateTime.MinValue;

        public bool CheckYoungHealTime()
        {
            if (DateTime.Now - m_LastYoungHeal > TimeSpan.FromMinutes(5.0))
            {
                m_LastYoungHeal = DateTime.Now;
                return true;
            }

            return false;
        }

        #endregion

        #region ShilCheckSkill

        public bool CheckSkill(SkillName skillName, int difficulty, int points)
        {
            // In case they have the skill arrow down
            if (difficulty == 0)
            {
                this.AwardSkillPoints(skillName, 0);
                return true;
            }

            if (difficulty < 0)
                return PercentSkillCheck(this, skillName, points);

            return DifficultySkillCheck(this, skillName, difficulty, points);
        }

        #endregion

        #region BuffManager

        private BuffManager m_BuffManager;
        public BuffManager BuffManager => m_BuffManager ??= new BuffManager(this);

        #endregion

        #region ZuluClass

        private ZuluClass m_ZuluClass;

        public ZuluClass ZuluClass
        {
            get => m_ZuluClass ??= new ZuluClass(this);
        }
        
        public ZuluClassType TargetZuluClass { get; set; }

        #endregion
        
        #region ZuluRace

        private ZuluRace m_ZuluRace;

        public ZuluRace ZuluRace
        {
            get => m_ZuluRace ??= new ZuluRace(this);
        }

        public ZuluRaceType ZuluRaceType { get; set; } = ZuluRaceType.None;

        #endregion

        #region Speech log

        public SpeechLog SpeechLog { get; private set; }

        public Point3D EarthPortalLocation { get; set; }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (SpeechLog.Enabled && NetState != null)
            {
                if (SpeechLog == null)
                    SpeechLog = new SpeechLog();

                SpeechLog.Add(e.Mobile, e.Speech);
            }
        }

        #endregion
        

        #region IElementalResistible

        [CommandProperty(AccessLevel.GameMaster)]
        public int WaterResist => this.GetResist(ElementalType.Water);

        [CommandProperty(AccessLevel.GameMaster)]
        public int AirResist => this.GetResist(ElementalType.Air);

        [CommandProperty(AccessLevel.GameMaster)]
        public int PhysicalResist => this.GetResist(ElementalType.Physical);

        [CommandProperty(AccessLevel.GameMaster)]
        public int FireResist => this.GetResist(ElementalType.Fire);

        [CommandProperty(AccessLevel.GameMaster)]
        public int EarthResist => this.GetResist(ElementalType.Earth);

        [CommandProperty(AccessLevel.GameMaster)]
        public int NecroResist => this.GetResist(ElementalType.Necro);

        [CommandProperty(AccessLevel.GameMaster)]
        public int ParalysisProtection => this.GetResist(ElementalType.Paralysis);

        [CommandProperty(AccessLevel.GameMaster)]
        public int HealingBonus => this.GetResist(ElementalType.HealingBonus);

        [CommandProperty(AccessLevel.GameMaster)]
        public PoisonLevel PoisonImmunity => (PoisonLevel) this.GetResist(ElementalType.Poison);

        [CommandProperty(AccessLevel.GameMaster)]
        public SpellCircle MagicImmunity => this.GetResist(ElementalType.MagicImmunity);

        [CommandProperty(AccessLevel.GameMaster)]
        public SpellCircle MagicReflection => this.GetResist(ElementalType.MagicReflection);

        #endregion
    }
}