using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace SunTools
{
    public class SunToolsGHInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "SunTools";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("befd983b-b005-431a-8811-7e112e4e6d87");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
