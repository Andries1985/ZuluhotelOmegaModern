namespace Server.Items
{
    public class Lute : BaseInstrument
	{

		[Constructible]
public Lute() : base( 0xEB3, 0x4C, 0x4D )
		{
			Weight = 5.0;
		}

		[Constructible]
public Lute( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( Weight == 3.0 )
				Weight = 5.0;
		}
	}
}
