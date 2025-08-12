using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DSAP.Models
{
    public class ShopLineUpItem
    {
        public ShopLineUpItemParam shopLineUpItemParam;
        public ulong startAddress;

        public ShopLineUpItem(ShopLineUpItemParam shopLineUpItemParam, ulong startAddress)
        {
            this.shopLineUpItemParam = shopLineUpItemParam;
            this.startAddress = startAddress;
        }
    }
}
