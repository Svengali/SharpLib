using System;

namespace lib.Net
{
	[Serializable]
	public class Msg
	{
		public Msg()
		{
		}
	}

	[Serializable]
	public class Login
	{
		public Login( String name, String pass )
		{
			m_username = name;
			m_password = pass;
		}

		public readonly String m_username;
		public readonly String m_password;
	}

	[Serializable]
	public class LoginResp
	{
		public LoginResp( bool resp )
		{
			m_resp = resp;
		}

		public readonly bool m_resp;
	}

	#region Admin Messages
	//Subclasses of this need to be on an admin client.
	[Serializable]
	public class Admin
	{

	};

	[Serializable]
	public class CreateEntity: Admin
	{

	}


	[Serializable]
	public class MoveEntity: Admin
	{

	}
	#endregion

	[Serializable]
	public class EntityBase
	{
		public EntityBase( int id )
		{
			m_id = id;
		}

		public readonly int m_id;
	};


	[Serializable]
	public class EntityPos: EntityBase
	{
		public EntityPos( int id, float x, float y, float z ) :
			base( id )
		{
			m_x = x;
			m_y = y;
			m_z = z;
		}

		public readonly float m_x;
		public readonly float m_y;
		public readonly float m_z;
	}

	[Serializable]
	public class EntityDesc: EntityBase
	{
		public EntityDesc( int id ) :
			base( id )
		{
		}

		//Should an entity have a mesh?  Be made up of multiple meshes?
		public readonly String m_mesh;
	}





}
