// Generated File. DO NOT MODIFY BY HAND.

namespace Server.Items
{
    public class CherryLog : BaseLog
    {
        [Constructible]
        public CherryLog() : this(1)
        {
        }


        [Constructible]
        public CherryLog(int amount) : base(CraftResource.Cherry, amount)
        {
        }

        [Constructible]
        public CherryLog(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int) 0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}