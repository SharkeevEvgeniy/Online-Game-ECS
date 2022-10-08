using MiniIT;

namespace MiniIT.Snipe
{
	public class SnipeRequestDescriptor
	{
		public string MessageType;
		public SnipeObject Data;
		
		public static implicit operator SnipeRequestDescriptor(string message_type)
		{
			return new SnipeRequestDescriptor()
			{
				MessageType = message_type,
			};
		}
	}
}