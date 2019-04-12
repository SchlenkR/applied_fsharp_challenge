
## Writing stateful processing functions

In the previous chapters, we wrote and composed pure (stateless) functions. This means that processing a signal (= a sequence of values) was made up of the following "recipe" (see chapter 1):
    * We have a sequence of input values.
    * We have a processing function.
    * That function is curried, so that after application of all parameters values, a function of float->float remains.
    * We have a "runtime" that generates an output sequence by mapping each value in the input sequence to a value in the output sequence by applying the processor function to an the input value.

[ 0.1  0.2  0.3 ]   input
   |    |    |
   |    |    |  --- Processing Function (here: multiply by 2)
   v    v    v
[ 0.2  0.4  0.6 ]   output

This technique if easy and sufficient for a lot of use cases, but not for all. Imagine this task:

### Low Pass Filter

"Apply a HPF (high pass filter) to a signal for 10 samples, then apply a LPF (low pass filter) to a signal for 10 samples, and so on."

There are several ways to achieve the desired behavior, and I would like to use the following "post-switch" approach as basis for the upcoming discussion:

We already have some building blocks for our task; the only one thing that is missing is the "counter" function to measure "10 samples".

The "counter" function can be described in this way:

"Write a function that outputs "1" for 10 samples. After these 10 samples, the output shall be made "0" for again 10 samples. After that, repeat from the beginning".

Having such a function, we can write a block diagram:

[Bs_C]

Now, there is a noticable thing:

Looking at the "counter" function used in the block diagram, you will notice that "counter" is special in a way that it has only a constant parameter (sample count of 10), but no input values available to transform; but it has an output value:

[ o  o  o  o  o  o  o  o ]   input
  |  |  |  |  |  |  |  |
  |  |  |  |  |  |  |  | --- transform_nothing_to_different_values
  v  v  v  v  v  v  v  v
[ 0  0  1  1  0  0  1  1 ]   output ('1's or '0's)

('o' means: no input value at all. In F#, this is represented by the **unit** type, that has an instance '()')

So here, we have a transformation from "nothing" to "something", and that "something" is not a constant; it differs from evaluation to evaluation. How can a function that has no information to work with evaluate to values that differ from each other? Answer: It can't. Conclusion: There must be some kind of (global, local, hidden, whatever) "state". In this case, it must be some kind of "local state" because the information needed is bound to that specific counter "instance".

### What is "state"?

This question leads to an interesting fundamental difference of the functional and imperative programming paradigms: In a 'pure' functional program, there is no concept of variables (or mutating state). For example, there is nothing like true randomness, which means: A function evaluates to the exact same value as in previous evaluations when the input values of that function are the same. A function itself cannot preserve state that is spanned over evaluations, and it cannot 'break out' of it's scope to call some random-number-generator or something else. From an inner perspective of a function, the whole 'universe' of that function is the values that are visible to it (by passing them or having values captured by closure). A function call has no identity; it doesn't remember or "preserve" anything from one evaluation to the next. At first view, this seems to be very limited compared to an imperative (including object oriented) approach, where repeatingly writing to and reading from known places (memory) is essential here. And this seems to be a good analogy to our real, physical world we live in: It's all a product of actio and reactio. So it seems like the functional paradigm doesn't fit well into the domain of audio DSP, where it's often about modeling physical processes, and it seems to be not the right approach for modeling our "counter" task where the current state is a product of all it's past states.

### An Object Oriented Approach

(You can find some more information in the appendix [here TODO].
<!-- In short, the important things are these: Locality implies that there must be some kind of "existence" of "things" over "time". So identity is made up of an address rather than by equal values. This is modeled by having addresses and pointer, which are accessible in a bigger context and preserved over a certain livetime. Then there is state that can be changed, because if it wouldn't be possible to change the instance state, the necessarity for having instances and pointers to them would not exist, because it wouldn't make a difference pointing to a thing or copying a thing. The behavior is the "gate" that everyone who wants to access (read or write) the instance state has to pass. This helps to ensure consistency during runtime by limiting access (mainly state changes) to special local places in the program. -->
)

So let's go for it using an object oriented approach at first and see where it leads us to.

"Object oriented programming is the thing with classes." I hear this sentence often, and it might seem true - not for all, but for the majority of mainstream OOP-languages. I think this doesn't capture the essence of object-oriented programming paradigm. A more basic definition of what OOP might be is when these 3 things occure together:

* References: Data is held in objects that have a referential identity (not a value-based identity).
* Mutability: Object values can change over time, while it's identity remains (objects are mutable).
* Encapsulation: Data access is protected via methods to help ensuring consistency during runtime (encapsulation of local state).

These three characteristics can be seen as features, but in the same time, we have to deal with their consequences, and this has significant impact on how we write code. The upcoming OOP-samples use non-class techniques with functions as objects, because that is closer to what we will end up with. The approach is still object-oriented due to the fact that they have an identity that encapsulates mutable state:

```csharp
public class FlipCounter
{
    private int currentCount;
    private bool isInSilentMode;

    public FlipCounter()
    {
        this.DesiredSampleLength = 10;
    }

    public int DesiredSampleLength { get; set; }

    public double ProcessNextValue()
    {
        if (this.currentCount >= this.DesiredSampleLength)
        {
            this.currentCount = 0;
            this.isInSilentMode = !this.isInSilentMode;
        }
        else
        {
            this.currentCount++;
        }

        return this.isInSilentMode ? 0.0 : 1.0;
    }
}
```

What we did:
    * mutable state + initialization
    * we update state in a whole pack by using anonymous classes: They are immutable and the compiler infers their type.
    * The factory returns a function that can be evaluated several times in the final computation
    * Problem: Nicht modulierbar

### Using the FlipCounter

TODO: Das stimmt nicht mehr; siehe oben::::::
Since we have a counter, we want to use it in a patch that works this way: Apply a HPF (high pass filter) to a signal for 10 samples, then apply a LPF (low pass filter) to a signal for 10 samples, and so on.

This can be expressed easily in a block diagram:

[TODO: Blockschaltbild]




*** Side Note
For this concrete example, there is an alternativ to the concept of local state: The counter function could be modeled in a way that it gets passed in a global "time" or "current sample count" parameter alongside with the current input value. That value can then be used to deduce the state of the counter. We will see later how we can pass that "reader" state into a processing functions. But when it comes to generator functions (chapter TODO) that have to have the ability of being modulated, a concept of "local state" is necessary.
***





<!-- 

(******* Exkurs 

[ 0.1; 0.2; 0.3 ]   input
\  | \  | \  |
 \ |  \ |  \ |  --- Processing Function (here: add last and current)
   v    v    v
[  ?; 0.3; 0.5 ]   output

********) -->






// TODO: Das hier in den Anhang schieben
Enough talk, let's get to the point: We want to model our counter using OOP - with examples in C# and classes:

```csharp


```

That's a lot of code for a problem that can be expressed so easily.

TODO: Erklären, was passiert
* Klassen brauchen wir nicht. Es geht auch so:
    * Ctor ist eine Factory
    * We could leave out isInSilentMode and deduce this state from currentCount.
    * WIr haben nur eine einzige Methode - gewissermaßen ein "Atom": An interface with 1 method can be represented by a function (interface is nominal, function is structural).



<!-- 

```csharp
public static Func<double> FlipCounter(int desiredSampleLength)
{
    var initialState = new
    {
        count = 0,
        isInSilentMode = false
    };

    var state = initialState;

    return new Func<double>(() =>
    {
        state =
            state.count >= desiredSampleLength
            ? initialState
            : new
                {
                    count = state.count + 1,
                    isInSilentMode = state.isInSilentMode
                };

        return state.isInSilentMode ? 0.0 : 1.0;
    });
}
``` -->