namespace Server.Items
{
    public class PhilosophersHat : WizardsHat
    {
        [Constructible]
        public PhilosophersHat()
        {
            LootType = LootType.Blessed;

            Attributes.RegenMana = 1;
            Attributes.LowerRegCost = 7;
        }

        public PhilosophersHat(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1077602; // Philosopher's Hat

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
