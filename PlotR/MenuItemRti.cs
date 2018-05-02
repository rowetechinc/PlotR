using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PlotR
{
    /// <summary>
    /// Menu item object.
    /// </summary>
    public class MenuItemRti
    {
        /// <summary>
        /// Header name.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Set if this menu item is checkable.
        /// </summary>
        public bool IsCheckable { get; set; }

        /// <summary>
        /// Set if the menu is checked or not.
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// Command.
        /// </summary>
        public ICommand Command { get; set; }

        /// <summary>
        /// Initialize value.
        /// </summary>
        public MenuItemRti()
        {
            Header = "";
            IsCheckable = false;
            IsChecked = false;
        }
    }
}
