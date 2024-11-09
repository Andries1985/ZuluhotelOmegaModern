// Generated File. DO NOT MODIFY BY HAND.

namespace Server.Items
{
    public class PurplePassionLog : BaseLog
    {
        [Constructible]
        public PurplePassionLog() : this(1)
        {
        }


        [Constructible]
        public PurplePassionLog(int amount) : base(CraftResource.PurplePassion, amount)
        {
        }

        [Constructible]
        public PurplePassionLog(Serial serial) : base(serial)
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