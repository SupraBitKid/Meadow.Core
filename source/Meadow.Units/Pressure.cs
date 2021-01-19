﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Meadow.Units.Conversions;

namespace Meadow.Units
{
    /// <summary>
    /// Represents temperature; the physical quantity that expresses hot and cold.
    /// It is the manifestation of thermal energy, present in all matter, which
    /// is the source of the occurrence of heat, a flow of energy, when a body
    /// is in contact with another that is colder or hotter.
    /// </summary>
    [Serializable]
    [ImmutableObject(true)]
    [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
    public struct Pressure : IComparable, IFormattable, IConvertible,
        IEquatable<double>, IComparable<double>
    {
        /// <summary>
        /// Creates a new `Pressure` object.
        /// </summary>
        /// <param name="value">The pressure value.</param>
        /// <param name="type">_Bar_ (`Bar`), by default.</param>
        public Pressure(double value, UnitType type = UnitType.Bar)
        {
            Value = value; Unit = type;
        }

        /// <summary>
        /// The temperature expressed as a value.
        /// </summary>
        [Pure] public double Value { get; }

        /// <summary>
        /// The unit that describes the value.
        /// </summary>
        [Pure] public UnitType Unit { get; }

        /// <summary>
        /// The type of units available to describe the temperature.
        /// </summary>
        public enum UnitType
        {
            Pascal,
            Psi,
            StandardAtmosphere,
            Bar
        }

        //========================
        // TO property conversions

        /// <summary>
        /// Gets the pressure value expressed as a unit _Bar_ (`Bar`)
        /// </summary>
        public double Bar {
            get {
                switch (Unit) {
                    case UnitType.Pascal:
                        return PressureConversions.PaToBar(Value);
                    case UnitType.Psi:
                        return PressureConversions.PsiToBar(Value);
                    case UnitType.StandardAtmosphere:
                        return PressureConversions.AtToBar(Value);
                    case UnitType.Bar:
                        return Value;
                    default: throw new Exception("the compiler lies.");
                }
            }
        }

        /// <summary>
        /// Gets the pressure value expressed as a unit _Pascal_ (`Pa`).
        /// </summary>
        public double Pascal {
            get {
                switch (Unit) {
                    case UnitType.Pascal:
                        return Value;
                    case UnitType.Psi:
                        return PressureConversions.PsiToPascal(Value);
                    case UnitType.StandardAtmosphere:
                        return PressureConversions.AtToPa(Value);
                    case UnitType.Bar:
                        return PressureConversions.BarToPa(Value);
                    default: throw new Exception("the compiler lies.");
                }
            }
        }

        /// <summary>
        /// Gets the pressure value expressed as a unit _Pound-force per square inch_ (`Psi`).
        /// </summary>
        public double Psi {
            get {
                switch (Unit) {
                    case UnitType.Pascal:
                        return PressureConversions.PaToPsi(Value);
                    case UnitType.Psi:
                        return Value;
                    case UnitType.StandardAtmosphere:
                        return PressureConversions.AtToPsi(Value);
                    case UnitType.Bar:
                        return PressureConversions.BarToPsi(Value);
                    default: throw new Exception("the compiler lies.");
                }
            }
        }

        /// <summary>
        /// Gets the pressure value expressed as a unit _Standard Atmosphere_ (`At`).
        /// </summary>
        public double StandardAtmosphere {
            get {
                switch (Unit) {
                    case UnitType.Pascal:
                        return PressureConversions.PaToAt(Value);
                    case UnitType.Psi:
                        return PressureConversions.PsiToAt(Value);
                    case UnitType.StandardAtmosphere:
                        return Value;
                    case UnitType.Bar:
                        return PressureConversions.BarToAt(Value);
                    default: throw new Exception("the compiler lies.");
                }
            }
        }

        //=============================
        // FROM convenience conversions

        /// <summary>
        /// Creates a new `Pressure` object from a unit value in _Bar_ (`Bar`).
        /// </summary>
        /// <param name="value">The pressure value.</param>
        /// <returns>A new pressure object.</returns>
        [Pure] public static Pressure FromBar(double value) => new Pressure(value, UnitType.Bar);

        /// <summary>
        /// Creates a new `Pressure` object from a unit value in _Pascal_ (`Pa`).
        /// </summary>
        /// <param name="value">The pressure value.</param>
        /// <returns>A new pressure object.</returns>
        [Pure] public static Pressure FromPascal(double value) => new Pressure(value, UnitType.Pascal);

        /// <summary>
        /// Creates a new `Pressure` object from a unit value in _Pounds-force per square inch_ (`Psi`).
        /// </summary>
        /// <param name="value">The pressure value.</param>
        /// <returns>A new temperature object.</returns>
        [Pure] public static Pressure FromPsi(double value) => new Pressure(value, UnitType.Psi);

        /// <summary>
        /// Creates a new `Pressure` object from a unit value in _Standard Atmosphere_ (`At`).
        /// </summary>
        /// <param name="value">The pressure value.</param>
        /// <returns>A new pressure object.</returns>
        [Pure] public static Pressure FromAt(double value) => new Pressure(value, UnitType.StandardAtmosphere);

        //=============================
        // Boilerplate interface stuff.

        [Pure]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) { return false; }
            if (Equals(this, obj)) { return true; }
            return obj.GetType() == GetType() && Equals((Pressure)obj);
        }

        [Pure] public bool Equals(Pressure other) => Value == other.Value;

        [Pure] public override int GetHashCode() => Value.GetHashCode();

        [Pure] public static bool operator ==(Pressure left, Pressure right) => Equals(left, right);
        [Pure] public static bool operator !=(Pressure left, Pressure right) => !Equals(left, right);
        [Pure] public int CompareTo(Pressure other) => Equals(this, other) ? 0 : Value.CompareTo(other.Value);
        [Pure] public static bool operator <(Pressure left, Pressure right) => Comparer<Pressure>.Default.Compare(left, right) < 0;
        [Pure] public static bool operator >(Pressure left, Pressure right) => Comparer<Pressure>.Default.Compare(left, right) > 0;
        [Pure] public static bool operator <=(Pressure left, Pressure right) => Comparer<Pressure>.Default.Compare(left, right) <= 0;
        [Pure] public static bool operator >=(Pressure left, Pressure right) => Comparer<Pressure>.Default.Compare(left, right) >= 0;

        [Pure] public static implicit operator Pressure(int value) => new Pressure(value);

        [Pure] public override string ToString() => Value.ToString();
        [Pure] public string ToString(string format, IFormatProvider formatProvider) => Value.ToString(format, formatProvider);

        // IComparable
        [Pure] public int CompareTo(object obj) => Value.CompareTo(obj);


        [Pure] public TypeCode GetTypeCode() => Value.GetTypeCode();
        [Pure] public bool ToBoolean(IFormatProvider provider) => ((IConvertible)Value).ToBoolean(provider);
        [Pure] public byte ToByte(IFormatProvider provider) => ((IConvertible)Value).ToByte(provider);
        [Pure] public char ToChar(IFormatProvider provider) => ((IConvertible)Value).ToChar(provider);
        [Pure] public DateTime ToDateTime(IFormatProvider provider) => ((IConvertible)Value).ToDateTime(provider);
        [Pure] public decimal ToDecimal(IFormatProvider provider) => ((IConvertible)Value).ToDecimal(provider);
        [Pure] public double ToDouble(IFormatProvider provider) => Value;
        [Pure] public short ToInt16(IFormatProvider provider) => ((IConvertible)Value).ToInt16(provider);
        [Pure] public int ToInt32(IFormatProvider provider) => ((IConvertible)Value).ToInt32(provider);
        [Pure] public long ToInt64(IFormatProvider provider) => ((IConvertible)Value).ToInt64(provider);
        [Pure] public sbyte ToSByte(IFormatProvider provider) => ((IConvertible)Value).ToSByte(provider);
        [Pure] public float ToSingle(IFormatProvider provider) => ((IConvertible)Value).ToSingle(provider);
        [Pure] public string ToString(IFormatProvider provider) => Value.ToString(provider);
        [Pure] public object ToType(Type conversionType, IFormatProvider provider) => ((IConvertible)Value).ToType(conversionType, provider);
        [Pure] public ushort ToUInt16(IFormatProvider provider) => ((IConvertible)Value).ToUInt16(provider);
        [Pure] public uint ToUInt32(IFormatProvider provider) => ((IConvertible)Value).ToUInt32(provider);
        [Pure] public ulong ToUInt64(IFormatProvider provider) => ((IConvertible)Value).ToUInt64(provider);

        [Pure]
        public int CompareTo(double? other)
        {
            return (other is null) ? -1 : ((IComparable<double>)Value).CompareTo(other.Value);
        }

        [Pure] public bool Equals(double? other) => Value.Equals(other);
        [Pure] public bool Equals(double other) => Value.Equals(other);
        [Pure] public int CompareTo(double other) => Value.CompareTo(other);
        // can't do this.
        //public int CompareTo(double? other) => Value.CompareTo(other);
    }
}
