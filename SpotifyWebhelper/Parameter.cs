using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpotifyWebhelper
{
    public class Parameter
    {
        public string name;
        public string value;

        public Parameter(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

    }
}
