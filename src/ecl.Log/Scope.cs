using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ecl.Log.Formatters;

namespace ecl.Log {
    class Scope : IDisposable {
        private readonly Scope _parent;
        private Logger _logger;

        public static readonly Scope Empty = new Scope( null );

        //public static Scope Empty = new Scope();
        public virtual object GetValue() {
            return null;
        }

        protected Scope( Logger logger ) {
            if ( logger != null ) {
                _logger = logger;
                _parent = Interlocked.Exchange( ref logger._current, this );
            }
        }
        public virtual void Dispose() {
            var logger = Interlocked.Exchange( ref _logger, null );
            if ( logger != null ) {
                // CompareExchange doesn't solve the un-disposed children scopes
                Interlocked.Exchange( ref logger._current, _parent );
            }
        }

        //public static Scope[] GetAll(Scope scope) {
        //    if ( scope == null )
        //        return Array.Empty<Scope>();

        //    List<Scope> list = new List<Scope>();
        //    do {
        //        list.Add( scope );
        //        scope = scope._parent;
        //    } while ( scope != null );
        //    list.Reverse();
        //    return list.ToArray();
        //}


        public void AppendTo( LoggerFormatter formatter ) {
            _parent?.AppendTo( formatter );
            AppendScope( formatter );
        }

        protected virtual void AppendScope( LoggerFormatter formatter ) {
        }
    }
    class StringScope : Scope {
        readonly string _state;

        public StringScope( Logger logger, string state )
            : base( logger ) {
            _state = state;
        }

        protected override void AppendScope( LoggerFormatter formatter ) {
            formatter.BeginScalar();
            formatter.Builder.Append( _state );
            formatter.CloseScalar();
        }
    }
    //class ScopeScalar : Scope {
    //    private readonly string _value;

    //    public ScopeScalar( Logger logger, string message )
    //        : base( logger ) {
    //        _value = message;
    //    }
    //    protected override void AppendScope( LoggerFormatter formatter ) {
    //        formatter.AppendScope( b, _value );
    //    }
    //    public override object GetValue() {
    //        return _value;
    //    }
    //}

    //class ScopeProperties : Scope {
    //    private IReadOnlyList<KeyValuePair<string, object>> _properties;

    //    public ScopeProperties( Logger logger, IReadOnlyList<KeyValuePair<string, object>> properties )
    //        : base( logger ) {
    //        _properties = properties;
    //    }
    //    protected override void AppendScope( Logger logger, StringBuilder b ) {
    //        logger.AppendScope( b, _properties );
    //    }
    //    public override object GetValue() {
    //        return _properties;
    //    }
    //}

    //class Scope<T> : Scope {
    //    public readonly T _state;

    //    public Scope( Logger logger, T state )
    //        : base( logger ) {
    //        _state = state;
    //    }

    //    public override object GetValue() {
    //        return _state;
    //    }


    //}
}
