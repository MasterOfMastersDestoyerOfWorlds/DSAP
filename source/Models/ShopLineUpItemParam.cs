using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DSAP.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
    public class ShopLineUpItemParam
    {
        // Per-item properties (Structure of Arrays)
        public int EquipId; // s32

        public int SoulValue; // s32

        public int MaterialId; // s32

        public int EventFlag; // s32 

        public int qwcId; // s32 dummy value 

        public short SellQuantity; //s16 -1 for infinite

        /**
            0 	Shop menu 	
            1 	Enhancement menu 	
            2 	Magic menu 	
            3 	Miracle menu 	
            4 	Information menu 	
            5 	SAN Value menu 
        **/
        public byte ShopType;

        /**
            0 	Weapon 	
            1 	Armor 	
            2 	Accessory 	
            3 	Good 	
            4 	Attunement 
        **/
        public byte EquipType;

        public long padding; //8 bytes
    }
}
