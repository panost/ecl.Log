using System;
namespace ecl.Log.Format {
    public interface IValueProvider {
        object GetValue( int index );
    }
}
