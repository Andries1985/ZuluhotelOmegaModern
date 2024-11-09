namespace Server.Items
{
    public class DispelFieldScroll : SpellScroll
    {
        [Constructible]
        public DispelFieldScroll() : this(1)
        {
        }


        [Constructible]
        public DispelFieldScroll(int amount) : base(Spells.SpellEntry.DispelField, 0x1F4E, amount)
        {
        }

        [Constructible]
        public DispelFieldScroll(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}