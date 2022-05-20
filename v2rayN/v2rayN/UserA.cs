using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

namespace vNet
{/// <summary>
 /// ListViewItem 排序
 ///int nSortCode = 1;  Mslist.ListViewItemSorter = new ListViewItemComparer(1, nSortCode);//1是按第几列排序
 ///         Mslist.Sort();
 ///         
 /// </summary>
    internal class FunA{
        public static bool MethodCanUse(string m)
        {
            bool can = false;

            switch (m)
            {
                case "auto":can= true;break;
                case "aes-256-gcm":can= true;break;
                case "aes-128-gcm":can= true;break;
                case "chacha20-poly1305": can= true;break;
                case "chacha20-ietf-poly1305": can= true;break;
                case "trojan": can= true;break;
                case "": can= true;break;
            }
            return can;
        }
    }
    class ListViewItemComparer : IComparer
    {
        private int col;
        private int code;

        public ListViewItemComparer(int nCol, int nCode)
        {
            col = nCol;
            code = nCode;
        }
       
        public int Compare(object x, object y)
        {
            int returnVal = -1;

            if (int.TryParse(((ListViewItem)x).SubItems[col].Text, out returnVal)
                && int.TryParse(((ListViewItem)y).SubItems[col].Text, out returnVal))
            {
                returnVal = int.Parse(((ListViewItem)x).SubItems[col].Text) > int.Parse(((ListViewItem)y).SubItems[col].Text) ? 1 : -1;
            }
            else
                returnVal = String.Compare(((ListViewItem)x).SubItems[col].Text,
                    ((ListViewItem)y).SubItems[col].Text);

            returnVal *= code;

            return returnVal;
        }
    }
}