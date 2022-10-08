
//  JSON support


using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;

namespace MiniIT
{
	public partial class SnipeObject
	{
		#region JSON

		public string ToJSONString()
		{
			return ConvertToJSONString(this);
		}

		public static SnipeObject FromJSONString(string input_string)
		{
			return FromJSONString(input_string, true);
		}

		public static SnipeObject FromJSONString(string input_string, bool strict)
		{
			return (SnipeObject)(new JSONDecoder(input_string, strict).getValue());
		}

		public static string ConvertToJSONString(object obj)
		{
			StringBuilder string_builder = new StringBuilder();
			ConvertToJSONString(obj, ref string_builder);
			return string_builder.ToString();
		}
		
		protected static void ConvertToJSONString(object obj, ref StringBuilder string_builder)
		{
			bool add_comma;

			if (obj is IDictionary<string, object> dict)
			{
				string_builder.Append("{");

				if (dict != null)
				{
					add_comma = false;
					foreach (string key in dict.Keys)
					{
						if (add_comma)
							string_builder.Append(",");
						else
							add_comma = true;

						if (key == "params")
							string_builder.Append("\"param\":"); // variable name "params" is reserved
						else
							string_builder.Append("\"" + key + "\":");

						object item = dict[key];
						ConvertToJSONString(item, ref string_builder);
					}
				}

				string_builder.Append("}");
			}
			else if (obj is string || obj is char)
			{
				string_builder.Append("\"");
				string convert = EscapeCommas(Convert.ToString(obj));
				// убираем из данных '\' и '"'
				convert = convert.Replace("\\", "\\\\");
				convert = convert.Replace("\"", "\\\"");
				string_builder.Append(convert);
				string_builder.Append("\"");
			}
			else if (obj is IEnumerable)
			{
				string_builder.Append("[");
				
				add_comma = false;
				foreach (object value in (IEnumerable)obj)
				{
					if (add_comma)
						string_builder.Append(",");
					else
						add_comma = true;
					
					ConvertToJSONString(value, ref string_builder);
				}
				
				string_builder.Append("]");
			}
			else if (obj != null && obj.GetType().IsPrimitive)
			{  
				string_builder.Append( Convert.ToString(obj, CultureInfo.InvariantCulture).ToLower() );
			}
			else
			{
				string_builder.Append("null");
			}
		}

		/// <summary>
		/// Escaping spesial characters including escaping of Unicode and ASCII non printable characters
		/// http://stackoverflow.com/a/14087738
		/// </summary>
		/// <returns>String with the special characters escaped</returns>
		/// <param name="input">string to process</param>
		/*
		protected static string ToLiteral(string input)
		{
			StringBuilder literal = new StringBuilder (input.Length + 2);
			literal.Append ("\"");
			foreach (var c in input)
			{
				switch (c)
				{
				case '\'':
					literal.Append (@"\'");
					break;
				case '\"':
					literal.Append ("\\\"");
					break;
				case '\\':
					literal.Append (@"\\");
					break;
				case '\0':
					literal.Append (@"\0");
					break;
				case '\a':
					literal.Append (@"\a");
					break;
				case '\b':
					literal.Append (@"\b");
					break;
				case '\f':
					literal.Append (@"\f");
					break;
				case '\n':
					literal.Append (@"\n");
					break;
				case '\r':
					literal.Append (@"\r");
					break;
				case '\t':
					literal.Append (@"\t");
					break;
				case '\v':
					literal.Append (@"\v");
					break;
				default:
					// ASCII printable character
					if (c >= 0x20 && c <= 0x7e)
					{
						literal.Append (c);
						// As UTF16 escaped character
					} else
					{
						literal.Append (@"\u");
						literal.Append (((int)c).ToString ("x4"));
					}
					break;
				}
			}
			literal.Append ("\"");
			return literal.ToString();
		}
		*/

		protected static string EscapeCommas(string input)
		{
			return input.Replace("\"", "\\\"");
		}



		#endregion
	}

	//-----------------------

	#region JSON Parser

	class JSONDecoder
	{
		private bool strict;
		
		/** The value that will get parsed from the JSON string */
		private object value;
		
		/** The tokenizer designated to read the JSON string */
		private JSONTokenizer tokenizer;
		
		/** The current token from the tokenizer */
		private JSONToken token;

		public JSONDecoder(string s, bool strict)
		{
			this.strict = strict;
			tokenizer = new JSONTokenizer(s,strict);
			nextToken();
			value = parseValue();
			if (strict && nextToken() != null)
				tokenizer.parseError("Unexpected characters left in input stream!");
		}

		public object getValue()
		{
			return value;
		}
		
		private JSONToken nextToken()
		{
			return token = tokenizer.getNextToken();
		}

		/**
		 * Attempt to parse an array
		 */
		private List<object> parseArray()
		{
			// create an array internally that we're going to attempt
			// to parse from the tokenizer
			List<object> a = new List<object>();
			// grab the next token from the tokenizer to move
			// past the opening [
			nextToken();
			// check to see if we have an empty array
			if ( token.type == JSONTokenType.RIGHT_BRACKET )
			{
				// we're done reading the array, so return it
				return a;
			}
			else
			{
				if (!strict && token.type == JSONTokenType.COMMA)
				{
					nextToken();
					// check to see if we're reached the end of the array
					if ( token.type == JSONTokenType.RIGHT_BRACKET )
					{
						return a;
					}
					else
					{
						tokenizer.parseError( "Leading commas are not supported. Expecting ']' but found " + token.value );
					}
				}
			}
			// deal with elements of the array, and use an "infinite"
			// loop because we could have any amount of elements
			while ( true )
			{
				// read in the value and add it to the array
				a.Add ( parseValue() );
				// after the value there should be a ] or a ,
				nextToken();			
				if ( token.type == JSONTokenType.RIGHT_BRACKET )
				{
					// we're done reading the array, so return it
					return a;
				}
				else if ( token.type == JSONTokenType.COMMA )
				{
					// move past the comma and read another value
					nextToken();
					// Allow arrays to have a comma after the last element
					// if the decoder is not in strict mode
					if ( !strict )
					{
						// Reached ",]" as the end of the array, so return it
						if ( token.type == JSONTokenType.RIGHT_BRACKET )
						{
							return a;
						}
					}
				}
				else
				{
					tokenizer.parseError( "Expecting ] or , but found " + token.value );
				}
			}
			//return null;
		}

		/**
		 * Attempt to parse an object
		 */
		private SnipeObject parseObject()
		{
			// create the object internally that we're going to
			// attempt to parse from the tokenizer
			SnipeObject o = new SnipeObject();
			// store the string part of an object member so
			// that we can assign it a value in the object
			string key;
			// grab the next token from the tokenizer
			nextToken();
			// check to see if we have an empty object
			if ( token.type == JSONTokenType.RIGHT_BRACE )
			{
				// we're done reading the object, so return it
				return o;
			}	// in non-strict mode an empty object is also a comma
			// followed by a right bracket
			else
			{
				if ( !strict && token.type == JSONTokenType.COMMA )
				{
					// move past the comma
					nextToken();				
					// check to see if we're reached the end of the object
					if ( token.type == JSONTokenType.RIGHT_BRACE )
					{
						return o;
					}
					else
					{
						tokenizer.parseError( "Leading commas are not supported.  Expecting '}' but found " + token.value );
					}
				}
			}
			// deal with members of the object, and use an "infinite"
			// loop because we could have any amount of members
			while ( true )
			{
				if ( token.type == JSONTokenType.STRING )
				{
					// the string value we read is the key for the object
					key = (string)(token.value);
					// move past the string to see what's next
					nextToken();
					// after the string there should be a :
					if ( token.type == JSONTokenType.COLON )
					{
						// move past the : and read/assign a value for the key
						nextToken();
						o[key] = parseValue();
						// move past the value to see what's next
						nextToken();
						// after the value there's either a } or a ,
						if ( token.type == JSONTokenType.RIGHT_BRACE )
						{
							// // we're done reading the object, so return it
							return o;						
						}
						else if ( token.type == JSONTokenType.COMMA )
						{
							// skip past the comma and read another member
							nextToken();
							
							// Allow objects to have a comma after the last member
							// if the decoder is not in strict mode
							if ( !strict )
							{
								// Reached ",}" as the end of the object, so return it
								if ( token.type == JSONTokenType.RIGHT_BRACE )
								{
									return o;
								}
							}
						}
						else
						{
							tokenizer.parseError( "Expecting } or , but found " + token.value );
						}
					}
					else 
					{
						tokenizer.parseError( "Expecting : but found " + token.value );
					}
				}
				else
				{
					tokenizer.parseError( "Expecting string but found " + token.value );
				}
			}
			//return null;
		}

		/**
		 * Attempt to parse a value
		 */
		private object parseValue()
		{
			// Catch errors when the input stream ends abruptly
			if ( token == null )
				tokenizer.parseError( "Unexpected end of input" );
			switch ( token.type )
			{
				case JSONTokenType.LEFT_BRACE:
					return parseObject();
				case JSONTokenType.LEFT_BRACKET:
					return parseArray();  // List<object> actually
				case JSONTokenType.STRING:
					return token.value;
				case JSONTokenType.NUMBER:
					return token.value;
				case JSONTokenType.TRUE:
					return true;
				case JSONTokenType.FALSE:
					return false;
				case JSONTokenType.NULL:
					return null;
				case JSONTokenType.NAN:
					if (!strict)
						return token.value;
					else
						tokenizer.parseError( "Unexpected " + token.value );
					break;
				default:
					tokenizer.parseError( "Unexpected " + token.value );
					break;
			}

			return null;
		}
	}

	internal enum JSONTokenType
	{
		UNKNOWN,
		COMMA,
		LEFT_BRACE,
		RIGHT_BRACE,
		LEFT_BRACKET,
		RIGHT_BRACKET,
		COLON,
		TRUE,
		FALSE,
		NULL,
		STRING,
		NUMBER,
		NAN
	}

	class JSONToken
	{
		/** type of the token */
		public JSONTokenType type;
		/** value of the token */
		public object value;
		
		/**
		 * Creates a new JSONToken with a specific token type and value.
		 *
		 * @param type The JSONTokenType of the token
		 * @param value The value of the token
		 */
		public JSONToken(JSONTokenType token_type, object value)
		{
			this.type = token_type;
			this.value = value;
		}

		public JSONToken(JSONTokenType token_type)
		{
			this.type = token_type;
			this.value = null;
		}

		public JSONToken()
		{
			this.type = JSONTokenType.UNKNOWN;
			this.value = null;
		}
	}


	class JSONTokenizer
	{
		/** The object that will get parsed from the JSON string */
		private SnipeObject obj;
		/** The JSON string to be parsed */
		private string jsonString;
		/** The current parsing location in the JSON string */
		private int loc;
		/** The current character in the JSON string during parsing */
		private char ch;
		
		private bool strict;

		public JSONTokenizer(string s, bool strict)
		{
			jsonString = s;
			this.strict = strict;
			loc = 0;
			// prime the pump by getting the first character
			nextChar();
		}


		/**
		 * Gets the next token in the input sting and advances
		 * the character to the next character after the token
		 */
		public JSONToken getNextToken()
		{
			JSONToken token = null;
			// skip any whitespace / comments since the last 
			// token was read
			skipIgnored();
			// examine the new character and see what we have...
			switch ( ch )
			{			
				case '{':
					token = new JSONToken(JSONTokenType.LEFT_BRACE,'{');
					nextChar();
					break;
				case '}':
					token = new JSONToken(JSONTokenType.RIGHT_BRACE, '}');
					nextChar();
					break;
				case '[':
					token = new JSONToken(JSONTokenType.LEFT_BRACKET, '[');
					nextChar();
					break;
				case ']':
					token = new JSONToken(JSONTokenType.RIGHT_BRACKET, ']');
					nextChar();
					break;
				case ',':
					token = new JSONToken(JSONTokenType.COMMA, ',');
					nextChar();
					break;
				case ':':
					token = new JSONToken(JSONTokenType.COLON, ':');
					nextChar();
					break;
				case 't': // attempt to read true
					string possibleTrue = "t" + nextChar() + nextChar() + nextChar();
					if ( possibleTrue == "true" )
					{
						token = new JSONToken(JSONTokenType.TRUE, true);
						nextChar();
					}
					else
					{
						parseError( "Expecting 'true' but found " + possibleTrue );
					}
					break;
				case 'f': // attempt to read false
					string possibleFalse = "f" + nextChar() + nextChar() + nextChar() + nextChar();
					if ( possibleFalse == "false" )
					{
						token = new JSONToken(JSONTokenType.FALSE, false);
						nextChar();
					}
					else
					{
						parseError( "Expecting 'false' but found " + possibleFalse );
					}
					break;
				case 'n': // attempt to read null
					string possibleNull = "n" + nextChar() + nextChar() + nextChar();
					if ( possibleNull == "null" )
					{
						token = new JSONToken(JSONTokenType.NULL, null);
						nextChar();
					}
					else
					{
						parseError( "Expecting 'null' but found " + possibleNull );
					}
					break;
				case 'N': //attempt to read NAN
					string possibleNAN = "N" + nextChar() + nextChar();
					if (possibleNAN == "NAN" || possibleNAN == "NaN")
					{
						token = new JSONToken(JSONTokenType.NAN, float.NaN);
						nextChar();
					}
					else
					{
						parseError("Expecting 'nan' but found " + possibleNAN);
					}
					break;
				case '"': // the start of a string
					token = readString();
					break;
				default: 
					// see if we can read a number
					if ( isDigit( ch ) || ch == '-' )
					{
						token = readNumber();
					}
					else if ( ch == 0 )
					{
						// check for reading past the end of the string
						return null;
					}
					else
					{
						// not sure what was in the input string - it's not
						// anything we expected
						parseError( "Unexpected " + ch + " encountered" );
					}
					break;
			}		
			return token;
		}

		/**
		 * Attempts to read a string from the input string.  Places
		 * the character location at the first character after the
		 * string.  It is assumed that ch is " before this method is called.
		 *
		 * @return the JSONToken with the string value if a string could
		 *		be read.  Throws an error otherwise.
		 */
		private JSONToken readString()
		{
			// the string to store the string we'll try to read
			string str = "";
			// advance past the first "
			nextChar();
			while ( ch != '"' && ch != 0 )
			{							
				//trace(ch);
				// unescape the escape sequences in the string
				if ( ch == '\\' )
				{		
					// get the next character so we know what
					// to unescape
					nextChar();				
					switch ( ch )
					{
						case '"': // quotation mark
							str += '"';
							break;
						case '/':	// solidus
							str += "/";
							break;
						case '\\':	// reverse solidus
							str += '\\';				
							break;
						case 'n':	// newline
							str += '\n';
							break;
						case 'r':	// carriage return
							str += '\r';
							break;
						case 't':	// horizontal tab
							str += '\t';
							break;
						case 'u':
							// convert a unicode escape sequence
							// to it's character value - expecting
							// 4 hex digits						
							// save the characters as a string we'll convert to an int
							string hexValue = "";
							// try to find 4 hex characters
							for (int i = 0; i<4; i++)
							{
								// get the next character and determine
								// if it's a valid hex digit or not
								if ( !isHexDigit( nextChar() ) )
								{
									parseError( " Excepted a hex digit, but found: " + ch );
								}
								// valid, add it to the value
								hexValue += ch;
							}
							// convert hexValue to an integer, and use that
							// integrer value to create a character to add
							// to our string.
							str += ((char)Convert.ToInt32(hexValue, 16)).ToString();
							break;

						default:
							// couldn't unescape the sequence, so just
							// pass it through
							str += '\\' + ch;
							break;
					}				
				}
				else
				{
					// didn't have to unescape, so add the character to the string
					str += ch;				
				}			
				// move to the next character
				nextChar();			
			}
			
			// we read past the end of the string without closing it, which
			// is a parse error
			if ( ch == 0 )
			{
				parseError( "Unterminated string literal" );
			}		
			// move past the closing " in the input string
			nextChar();		
			// the token for the string we'll try to read
			JSONToken token = new JSONToken();
			token.type = JSONTokenType.STRING;
			// attach to the string to the token so we can return it
			token.value = str;		
			return token;
		}


		/**
		 * Attempts to read a number from the input string.  Places
		 * the character location at the first character after the
		 * number.
		 * 
		 * @return The JSONToken with the number value if a number could
		 * 		be read.  Throws an error otherwise.
		 */
		private JSONToken readNumber()
		{
			// the string to accumulate the number characters
			// into that we'll convert to a number at the end
			string input = "";		
			// check for a negative number
			if ( ch == '-' )
			{
				input += '-';
				nextChar();
			}		
			// the number must start with a digit
			if ( !isDigit( ch ) )
			{
				parseError( "Expecting a digit" );
			}		
			// 0 can only be the first digit if it
			// is followed by a decimal point
			if ( ch == '0' )
			{
				input += ch;
				nextChar();			
				// make sure no other digits come after 0
				if ( isDigit( ch ) )
				{
					parseError( "A digit cannot immediately follow 0" );
				}
				// unless we have 0x which starts a hex number, but this
				// doesn't match JSON spec so check for not strict mode.
				else
				{
					if (!strict && ch == 'x')
					{
						// include the x in the input
						input += ch;
						nextChar();
						// need at least one hex digit after 0x to
						// be valid
						if (isHexDigit(ch))
						{
							input += ch;
							nextChar();
						}
						else
						{
							parseError( "Number in hex format require at least one hex digit after \"0x\"" );
						}
						// consume all of the hex values
						while (isHexDigit(ch))
						{
							input += ch;
							nextChar();
						}
						input = Convert.ToInt32(input, 16).ToString();
					}
				}
			}
			else
			{
				// read numbers while we can
				while ( isDigit( ch ) )
				{
					input += ch;
					nextChar();
				}
			}		
			// check for a decimal value
			if ( ch == '.' )
			{
				input += '.';
				nextChar();
				// after the decimal there has to be a digit
				if ( !isDigit( ch ) )
				{
					parseError( "Expecting a digit" );
				}
				// read more numbers to get the decimal value
				while ( isDigit( ch ) )
				{
					input += ch;
					nextChar();
				}
			}
			// check for scientific notation
			if ( ch == 'e' || ch == 'E' )
			{
				input += "e";
				nextChar();
				// check for sign
				if ( ch == '+' || ch == '-' )
				{
					input += ch;
					nextChar();
				}
				// require at least one number for the exponent
				// in this case
				if ( !isDigit( ch ) )
				{
					parseError( "Scientific notation number needs exponent value" );
				}
				// read in the exponent
				while ( isDigit( ch ) )
				{
					input += ch;
					nextChar();
				}
			}
			// convert the string to a number value
			int int_num;
			bool correct_int = false;
			try
			{
				int_num = Convert.ToInt32(input);
				correct_int = true;
			}
			catch(FormatException)
			{
				int_num = 0;
				correct_int = false;
			}

			long long_num;
			bool correct_long = false;
			try
			{
				long_num = Convert.ToInt64(input);
				correct_long = true;
			}
			catch(FormatException)
			{
				long_num = 0;
				correct_long = false;
			}

			float float_num = Convert.ToSingle(input, CultureInfo.InvariantCulture);
			if ( !float.IsInfinity( float_num ) && !float.IsNaN( float_num ) )
			{
				// the token for the number we'll try to read
				JSONToken token = new JSONToken();
				token.type = JSONTokenType.NUMBER;

				if (correct_long && float_num == Convert.ToSingle(long_num) ) // the number is integer
				{
					if (correct_int && long_num == Convert.ToInt64(int_num) ) // the number is int32
					{
						token.value = int_num; // boxing int to object
					}
					else  // the number is int64
					{
						token.value = long_num; // boxing long int to object
					}
				}
				else  // the number is float
				{
					token.value = float_num;
				}
				return token;
			}
			else
			{
				parseError( "Number " + float_num + " is not valid!" );
			}
			return null;
		}


		/**
		 * Reads the next character in the input
		 * string and advances the character location.
		 *
		 * @return The next character in the input string, or
		 *		null if we've read past the end.
		 */
		private char nextChar()
		{
			if (loc < jsonString.Length)
				return ch = jsonString[loc++];
			else
				return ch = (char)0;
		}

		/**
		 * Advances the character location past any
		 * sort of white space and comments
		 */
		private void skipIgnored()
		{
			int originalLoc;
			// keep trying to skip whitespace and comments as long
			// as we keep advancing past the original location 
			do
			{
				originalLoc = loc;
				skipWhite();
				skipComments();
			} while ( originalLoc != loc );
		}

		/**
		 * Skips comments in the input string, either
		 * single-line or multi-line.  Advances the character
		 * to the first position after the end of the comment.
		 */
		private void skipComments()
		{
			if ( ch == '/' )
			{
				// Advance past the first / to find out what type of comment
				nextChar();
				switch ( ch )
				{
					case '/': // single-line comment, read through end of line					
						// Loop over the characters until we find
						// a newline or until there's no more characters left
						do
						{
							nextChar();
						} while ( ch != '\n' && ch != 0 );
						// move past the \n
						nextChar();
						goto case '*';

					case '*': // multi-line comment, read until closing */
						// move past the opening *
						nextChar();
						// try to find a trailing */
						while ( true )
						{
							if ( ch == '*' )
							{
								// check to see if we have a closing /
								nextChar();
								if ( ch == '/')
								{
									// move past the end of the closing */
									nextChar();
									break;
								}
							}
							else
							{
								// move along, looking if the next character is a *
								nextChar();
							}
							// when we're here we've read past the end of 
							// the string without finding a closing */, so error
							if ( ch == 0 )
							{
								parseError( "Multi-line comment not closed" );
							}
						}
						break;
						// Can't match a comment after a /, so it's a parsing error
					default:
						parseError( "Unexpected \"" + ch + "\" encountered (expecting '/' or '*' )" );
						break;
				}
			}		
		}

		/**
		 * Skip any whitespace in the input string and advances
		 * the character to the first character after any possible
		 * whitespace.
		 */
		private void skipWhite()
		{		
			// As long as there are spaces in the input 
			// stream, advance the current location pointer
			// past them
			while ( isWhiteSpace( ch ) ) {
				nextChar();
			}		
		}
		
		/**
		 * Determines if a character is whitespace or not.
		 *
		 * @return True if the character passed in is a whitespace
		 *	character
		 */
		private bool isWhiteSpace( char ch )
		{
			return ( ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r' );
		}
		
		/**
		 * Determines if a character is a digit [0-9].
		 *
		 * @return True if the character passed in is a digit
		 */
		private bool isDigit( char ch )
		{
			return ( ch >= '0' && ch <= '9' );
		}
		
		/**
		 * Determines if a character is a digit [0-9].
		 *
		 * @return True if the character passed in is a digit
		 */
		private bool isHexDigit( char ch )
		{
			// get the uppercase value of ch so we only have
			// to compare the value between 'A' and 'F'
			char uc = ch.ToString().ToUpper()[0];
			// a hex digit is a digit of A-F, inclusive ( using
			// our uppercase constraint )
			return ( isDigit( ch ) || ( uc >= 'A' && uc <= 'F' ) );
		}

		/**
		 * Raises a parsing error with a specified message, tacking
		 * on the error location and the original string.
		 *
		 * @param message The message indicating why the error occurred
		 */
		public void parseError( string message )
		{
			throw new Exception( "SnipeObjectJSONParserError : " + message + " at position: " + loc + " near \"" + jsonString+"\"" );
		}
	}

	#endregion
}