using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace mod
{

	[Serializable]
	public class Config : lib.Config
	{
		public String name = "Generic";
	}

	public class View
	{
	}

	public class Base
	{
		public Config Cfg { get { return m_cfg; } }

		public Base( Config cfg )
		{
			m_cfg = cfg;
		}

		private Config m_cfg;
	}


	[Serializable]
	public class FluidConfig : Config
	{
		public String type = "none";
	}


	public class FluidBase : Base
	{
		public new FluidConfig Cfg { get { return (FluidConfig)base.Cfg; } }

		public FluidBase( FluidConfig cfg )
			: base( cfg )
		{
		}
	}
















	[Serializable]
	public class SystemConfig : Config
	{
		public String type = "none";
	}


	public class System
	{
		public SystemConfig Cfg { get { return m_cfg; } }

		public System( SystemConfig cfg )
		{
			m_cfg = cfg;
		}

		private SystemConfig m_cfg;
	}


}
