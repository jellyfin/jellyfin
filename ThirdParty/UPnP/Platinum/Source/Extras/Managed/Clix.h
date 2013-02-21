// ------------------------------------------------------------------------------------------- //
// clix.h
//
// http://www.nuclex.org/articles/5-cxx/10-marshaling-strings-in-cxx-cli
//
// Marshals strings between .NET and C++ using C++/CLI (Visual C++ 2005 and later only).
// Faster and cleaner than the System::Interop method because it uses garbage collected memory.
// Use at your own leisure. No warranties whatsoever provided.
//
// Original code by Markus Ewald (http://www.nuclex.org/articles/marshaling-strings-in-cxx-cli)
// Updated version including several improvements suggested by Neil Hunt
// ------------------------------------------------------------------------------------------- //
#pragma once
 
#include <string>
#include <vcclr.h>
 
// CLI extensions namespace
namespace clix {
 
  /// <summary>Encoding types for strings</summary>
  enum Encoding {
     
    /// <summary>ANSI encoding</summary>
    /// <remarks>
    ///   This is the default encoding you've most likely been using all around in C++. ANSI
    ///   means 8 Bit encoding with character codes depending on the system's selected code page.
    /// <remarks>
    E_ANSI,

    /// <summary>UTF-8 encoding</summary>
    /// <remarks>
    ///   This is the encoding commonly used for multilingual C++ strings. All ASCII characters
    ///   (0-127) will be represented as single bytes. Be aware that UTF-8 uses more than one
    ///   byte for extended characters, so std::string::length() might not reflect the actual
    ///   length of the string in characters if it contains any non-ASCII characters.
    /// <remarks>
    E_UTF8,

    /// <summary>UTF-16 encoding</summary>
    /// <remarks>
    ///   This is the suggested to be used for marshaling and the native encoding of .NET
    ///   strings. It is similar to UTF-8 but uses a minimum of two bytes per character, making
    ///   the number of bytes required for a given string better predictable. Be aware, however,
    ///   that UTF-16 can still use more than two bytes for a character, so std::wstring::length()
    ///   might not reflect the actual length of the string.
    /// </remarks>
    E_UTF16, E_UNICODE = E_UTF16

  };

  // Ignore this if you're just scanning the headers for informations :-)
  /* All this template stuff might seem like overkill, but it is well thought out and enables
     you to use a readable and convenient call while still keeping the highest possible code
     efficiency due to compile-time evaluation of the required conversion path.
  */
  namespace detail {
     
    // Get C++ string type for specified encoding
    template<Encoding encoding> struct StringTypeSelecter;
    template<> struct StringTypeSelecter<E_ANSI> { typedef std::string Type; };
    template<> struct StringTypeSelecter<E_UTF8> { typedef std::string Type; };
    template<> struct StringTypeSelecter<E_UTF16> { typedef std::wstring Type; };

    // Compile-time check whether a given type is a managed System::String
    template<typename StringType> struct IsManagedString { enum { Result = false }; };
    template<> struct IsManagedString<System::String ^> { enum { Result = true }; };

    // Compile-time selection of two types depending on a boolean expression
    template<bool expression> struct Select;
    template<> struct Select<false> {
      template<typename TrueType, typename FalseType> struct Type { typedef FalseType Result; };
    };
    template<> struct Select<true> {
      template<typename TrueType, typename FalseType> struct Type { typedef TrueType Result; };
    };

    // Direction of the marshaling process
    enum MarshalingDirection {
      CxxFromNet,
      NetFromCxx
    };

    // The actual marshaling code
    template<MarshalingDirection direction> struct StringMarshaler;

    // Marshals to .NET from C++ strings
    template<> struct StringMarshaler<NetFromCxx> {

      template<Encoding encoding, typename SourceType>
      static System::String ^marshal(const SourceType &string) {
        // Constructs a std::[w]string in case someone gave us a char * to choke on
        return marshalCxxString<encoding, SourceType>(string);
      }
      
      template<Encoding encoding, typename SourceType>
      static System::String ^marshalCxxString(
        const typename StringTypeSelecter<encoding>::Type &cxxString
      ) {
        typedef typename StringTypeSelecter<encoding>::Type SourceStringType;
        size_t byteCount = cxxString.length() * sizeof(SourceStringType::value_type);

        // Copy the C++ string contents into a managed array of bytes
        array<unsigned char> ^bytes = gcnew array<unsigned char>(byteCount);
        { pin_ptr<unsigned char> pinnedBytes = &bytes[0];
          memcpy(pinnedBytes, cxxString.c_str(), byteCount);
        }

        // Now let one of .NET's encoding classes do the rest
        return decode<encoding>(bytes);
      }

      private:
        // Converts a byte array based on the selected encoding
        template<Encoding encoding> static System::String ^decode(array<unsigned char> ^bytes);
        template<> static System::String ^decode<E_ANSI>(array<unsigned char> ^bytes) {
          return System::Text::Encoding::Default->GetString(bytes);
        }
        template<> static System::String ^decode<E_UTF8>(array<unsigned char> ^bytes) {
          return System::Text::Encoding::UTF8->GetString(bytes);
        }
        template<> static System::String ^decode<E_UTF16>(array<unsigned char> ^bytes) {
          return System::Text::Encoding::Unicode->GetString(bytes);
        }
    };

    // Marshals to C++ strings from .NET
    template<> struct StringMarshaler<CxxFromNet> {

      template<Encoding encoding, typename SourceType>
      static typename detail::StringTypeSelecter<encoding>::Type marshal(
        System::String ^string
      ) {
        typedef typename StringTypeSelecter<encoding>::Type StringType;

        // First, we use .NET's encoding classes to convert the string into a byte array
        array<unsigned char> ^bytes = encode<encoding>(string);

        // fix crash if empty string passed
        if (bytes->Length == 0) return StringType();

        // Then we construct our native string from that byte array
        pin_ptr<unsigned char> pinnedBytes(&bytes[0]);
        return StringType(
          reinterpret_cast<StringType::value_type *>(static_cast<unsigned char *>(pinnedBytes)),
          bytes->Length / sizeof(StringType::value_type)
        );
      }

      template<> static std::wstring marshal<E_UTF16, System::String ^>(
        System::String ^string
      ) {
        // fix crash if empty string passed
        if (string->Length == 0) return std::wstring();

        // We can directly access the characters in the managed string
        pin_ptr<const wchar_t> pinnedChars(::PtrToStringChars(string));
        return std::wstring(pinnedChars, string->Length);
      }
 
      private:
        // Converts a string based on the selected encoding
        template<Encoding encoding> static array<unsigned char> ^encode(System::String ^string);
        template<> static array<unsigned char> ^encode<E_ANSI>(System::String ^string) {
          return System::Text::Encoding::Default->GetBytes(string);
        }
        template<> static array<unsigned char> ^encode<E_UTF8>(System::String ^string) {
          return System::Text::Encoding::UTF8->GetBytes(string);
        }
        template<> static array<unsigned char> ^encode<E_UTF16>(System::String ^string) {
          return System::Text::Encoding::Unicode->GetBytes(string);
        }

    };

  } // namespace detail
     
  // ----------------------------------------------------------------------------------------- //
  // clix::marshalString()
  // ----------------------------------------------------------------------------------------- //
  /// <summary>Marshals strings between .NET managed and C++ native</summary>
  /// <remarks>
  ///   This all-in-one function marshals native C++ strings to .NET strings and vice versa.
  ///   You have to specify an encoding to use for the conversion, which always applies to the
  ///   native C++ string as .NET always uses UTF-16 for its own strings.
  /// </remarks>
  /// <param name="string">String to be marshalled to the other side</param>
  /// <returns>The marshaled representation of the string</returns>
  template<Encoding encoding, typename SourceType>
  typename detail::Select<detail::IsManagedString<SourceType>::Result>::Type<
    typename detail::StringTypeSelecter<encoding>::Type,
    System::String ^
  >::Result marshalString(SourceType string) {
   
    // Pass on the call to our nifty template routines
    return detail::StringMarshaler<
      detail::IsManagedString<SourceType>::Result ? detail::CxxFromNet : detail::NetFromCxx
    >::marshal<encoding, SourceType>(string);
   
  }

} // namespace clix
