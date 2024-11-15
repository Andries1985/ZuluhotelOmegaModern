using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBJewel: SBInfo
	{
		private List<GenericBuyInfo> m_BuyInfo = new InternalBuyInfo();
		private IShopSellInfo m_SellInfo = new InternalSellInfo();

		public SBJewel()
		{
		}

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
		public override List<GenericBuyInfo> BuyInfo { get { return m_BuyInfo; } }

		public class InternalBuyInfo : List<GenericBuyInfo>
		{
			public InternalBuyInfo()
			{
				Add( new GenericBuyInfo( typeof( GoldRing ), 27, 20, 0x108A, 0 ) );
				Add( new GenericBuyInfo( typeof( Necklace ), 26, 20, 0x1085, 0 ) );
				Add( new GenericBuyInfo( typeof( GoldNecklace ), 27, 20, 0x1088, 0 ) );
				Add( new GenericBuyInfo( typeof( GoldBeadNecklace ), 27, 20, 0x1089, 0 ) );
				Add( new GenericBuyInfo( typeof( Beads ), 27, 20, 0x108B, 0 ) );
				Add( new GenericBuyInfo( typeof( GoldBracelet ), 27, 20, 0x1086, 0 ) );
				Add( new GenericBuyInfo( typeof( GoldEarrings ), 27, 20, 0x1087, 0 ) );

				Add( new GenericBuyInfo( "1060740", typeof( BroadcastCrystal ),  68, 20, 0x1ED0, 0, new object[] {  500 } ) ); // 500 charges
				Add( new GenericBuyInfo( "1060740", typeof( BroadcastCrystal ), 131, 20, 0x1ED0, 0, new object[] { 1000 } ) ); // 1000 charges
				Add( new GenericBuyInfo( "1060740", typeof( BroadcastCrystal ), 256, 20, 0x1ED0, 0, new object[] { 2000 } ) ); // 2000 charges

				Add( new GenericBuyInfo( "1060740", typeof( ReceiverCrystal ), 6, 20, 0x1ED0, 0 ) );

				Add( new GenericBuyInfo( typeof( StarSapphire ), 125, 20, 0xF21, 0 ) );
				Add( new GenericBuyInfo( typeof( Emerald ), 100, 20, 0xF10, 0 ) );
				Add( new GenericBuyInfo( typeof( Sapphire ), 100, 20, 0xF19, 0 ) );
				Add( new GenericBuyInfo( typeof( Ruby ), 75, 20, 0xF13, 0 ) );
				Add( new GenericBuyInfo( typeof( Citrine ), 50, 20, 0xF15, 0 ) );
				Add( new GenericBuyInfo( typeof( Amethyst ), 100, 20, 0xF16, 0 ) );
				Add( new GenericBuyInfo( typeof( Tourmaline ), 75, 20, 0xF2D, 0 ) );
				Add( new GenericBuyInfo( typeof( Amber ), 50, 20, 0xF25, 0 ) );
				Add( new GenericBuyInfo( typeof( Diamond ), 200, 20, 0xF26, 0 ) );

			}
		}

		public class InternalSellInfo : GenericSellInfo
		{
			public InternalSellInfo()
			{
				Add( typeof( Amber ), 5 );
				Add( typeof( Amethyst ), 10 );
				Add( typeof( Citrine ), 5 );
				Add( typeof( Diamond ), 20 );
				Add( typeof( Emerald ), 10 );
				Add( typeof( Ruby ), 7 );
				Add( typeof( Sapphire ), 10 );
				Add( typeof( StarSapphire ), 12 );
				Add( typeof( Tourmaline ), 7 );
				Add( typeof( GoldRing ), 2 );
				Add( typeof( SilverRing ), 2 );
				Add( typeof( Necklace ), 2 );
				Add( typeof( GoldNecklace ), 2 );
				Add( typeof( GoldBeadNecklace ), 2 );
				Add( typeof( SilverNecklace ), 2 );
				Add( typeof( SilverBeadNecklace ), 2 );
				Add( typeof( Beads ), 2 );
				Add( typeof( GoldBracelet ), 2 );
				Add( typeof( SilverBracelet ), 2 );
				Add( typeof( GoldEarrings ), 2 );
				Add( typeof( SilverEarrings ), 2 );
			}
		}
	}
}
