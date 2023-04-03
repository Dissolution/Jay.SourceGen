###### Implementable Interfaces
- `IEquatable<TSelf>`
  - requires [Equatable]/[Key]/??? Attribute or defaults to Record?
  - also implements `==` and `!=`
  - effects `GetHashCode()` and `Equals(object?)`
- `IComparable<TSelf>`
  - requires [Comparable]/[Key]/??? Attribute or defaults to Record?
  - also implements `>`, `<`, `>=`, `<=`
- `IDisposable`
  - requires [Dispose] attribute on Field(s), Property(s), and/or Event(s)
- `INotifyPropertyChanged` + `INotifyPropertyChanging`
  - auto-implemented for all Public Properties
  - both are implemented if either are
- `IFormattable`
  - Supports custom formatting?
- `ICloneable`
  - 

##### Methods
- Most of the above will effect the output of `ToString()`
- Key members will be formatted in the output (as per `record`)
- Can be effected by [Display]?

### THOTS+PLAYERZ

- Flow:
  1. Build up a list of all `Properties`, `Events`, and `Methods` (others?) that need to be implemented
     1. Store in a `Dict<MemberSig, IImplementer?>`
     2. Scan all `Interfaces` (be sure it's all the way down the nested hierarchy)
        1. For each, find an `IInterfaceImplementer`
           - If you can't, `throw`
        2. (.5?) Scan for Properties/Events/Methods
        3. That `Implementer` may have additional data we need later (overrides)
        4. It will add to that `Dict` methods with itself as the Implementer
        5. Ditto for `Constructors` (I know, they _are_ methods)
        6. It will add `Properties` to be implemented (complex!)
        7. It will add `Events` to be implemented (ditto)
     3. Do a post-scan for all `Members` with a null `Implementer`
        1. Properties -> An assembled Implementer
        2. Events -> (see above)
        3. `throw`



- `IPropertyGen`
  - `IFieldGen`
    - `Func<PropertySig, FieldSig> FieldCreator;`
    - `Action<FieldSig, Context> FieldWriter;`
  - `IGetterGen`
    - `Action<PropertySig, Context> GetterWriter;`
    - Sub-Section Writers?
      - `Action<???>? PreGet`
      - `Func<???, T> Get`
      - `Action<???>? PostGet`
        - If `!null`, then we need a way to pull the `T` from `Get` and return it after the `PostGet` operation (and possible send to the `PostGet` operation?)
  - `ISetterGen`
    - `Action<PropertySig, Context> SetterWriter;`
    - Sub-Section Writers? -- `Context: Field!`
      - `Action<???>? PreSet`
      - `Action<???, T> Set`
      - `Action<???>? PostSet`



- Func<Property, string> FieldNameGen
  - As a class, controls Vis + Keywords?
- Func<Property, ??> GetterGen
- Func<Property, ??> SetterGen
- Method sections?
  - _pre_-Get/Set
  - Get/Set (might be direct field, might be SetField, etc)
  - _post_-Get/Set
    - Might need a post-post for a return callback for a getter?
    - Some way of saying 'no-post'->shortcut?

