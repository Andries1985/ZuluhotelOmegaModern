using Server.Spells;

namespace Server.Items
{
    public class EarthSpellScroll : CustomSpellScroll
    {
        [Constructible]
        public EarthSpellScroll(SpellEntry spellEntry, int itemId, int amount) : base(spellEntry, itemId, amount, 0x48A)
        {
        }

        [Constructible]
        public EarthSpellScroll(Serial serial) : base(serial)
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