
## Writing stateful processing functions

In the previous chapters, we wrote and composed pure (stateless) functions. This means that processing a signal (= a sequence of values) was made up of the following "recipe" (see chapter 1):
    * We have a sequence of input values
    * We have a processing function in form of float->float
    * We have a "runtime" that generates an output sequence by mapping each value in the input sequence to a value in the output sequence by applying the processor function to an the input value.

[ 0.1; 0.2; 0.3 ]   input
   |    |    |
   |    |    |  --- Processing Function (here: multiply by 2)
   v    v    v
[ 0.2; 0.4; 0.6 ]   output

This technique if easy and sufficient for a lot of use cases, but not for all. Imagine this task:

### Counter

"Write a function that outputs "1" for 10 samples. After these n samples, the output shall be made "0" for again n samples. After that, repeat from the beginning".

The issue here is that the processing function has to somehow "memory" how many samples it led through or if it is in "silent" mode. We consider this information and "local state".

***
Note: For this concrete example, there is an alternativ to the concept of local state: The counter function could be modeled in a way that it gets passed in a global "time" or "current sample count" parameter alongside with the current input value. That value can then be used to deduce the state of the counter. We will see later how we can pass that "reader" state into a processing functions. But when it somes to generator functions (chapter TODO) that have to have the ability of being modulated, a concept of "local state" is necessary.
***

How can "local state" be interpreted - what exactly is that? This question leads to an interesting fundamental difference of the functional and imperative programming paradigms: In a 'pure' functional program, there is no concept of variables (or mutating state). For example, There is nothing like true randomness which means: A function evaluates to the exact same value as in previous evaluations when the input values of that function are the same. A function itself cannot preserve state that is spanned over evaluations, and it cannot 'break out' of it's scope to call some random-number-generator or something else. From an inner perspective of a function, the whole 'universe' of that function is the values that are visible to it (by passing them or having values captured by closure). This function has no identity; it doesn't remember or "preserve" anything from one evaluation to the next. At first view, this seems to be very limited compared to an imperative (including object oriented) approach: Repeatingly writing to and reading from known places (memory) is essential here. And this seems to be a good analogy to our real, physical world we live in: It's all a product of actio and reactio. So it seems like the functional paradigm doesn't fit well into the domain of audio DSP, where it's often about modeling physical processes, and it seemd to be not the right approach for modelingour "counter" task where the current state is a product of all it's past states.

So let's go for it using an object oriented approach at first and see where it leads us to.

### An Object Oriented Approach

"Object oriented programming is the thing with classes." I hear this sentence often, and it might seem true not for all, but for the majority of mainstream OO-languages. I think this doesn't capture the essence of object-oriented programming paradigm. A thesis of what OOP might be is this:

"OOP is about encapsulating (hiding / protecting) local state by behavior". So there are 3 things involved: Locality, protection and behavior. From here, one could start an interesting, controverse discussion of this it true or not, and which implication this brings. In short, the important things are these: Locality implies that there must be some kind of "existence" of "things" over "time". So identity is made up of an address rather than by equal values. This is modeled by having addresses and pointer to them, which are accessible in a bigger context and preserved over a certain livetime. Then there is state that can be changed, because it it wouldn't be possible to change the instance state, the necessarity for having instances and pointers to them would not exist, because it wouldn't make a difference pointing to a thing or copying a thing. The behavior is the "gate" that everyone who wants to access (read or write) the instance state has to pass. This helps to ensure consistent runtime values by limiting state changes to special places in the program.

Reading the above thesis, you see that "classes" or "inheritance" don't appear at all, and there are indeed other techniques than classes that fulfill it.

Enough talk, let's get to the point: We want to model our counter using OOP - with examples in C# and classes:

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

That's a lot of code for a problem that can be expressed so easily.

TODO: Erklären, was passiert
* Klassen brauchen wir nicht. Es geht auch so:
    * Ctor ist eine Factory
    * We could leave out isInSilentMode and deduce this state from currentCount.
    * WIr haben nur eine einzige Methode - gewissermaßen ein "Atom": An interface with 1 method can be represented by a function (interface is nominal, function is structural).


// TODO: Das hier in den Anhang schieben; wir machen alles mit Klassen; Note machen, dass das im Anhang steht und eine Alternative ist.
    #### Non-classical approach

    ```csharp
    public static Func<double> FlipCounter(int desiredSampleLength)
    {
        var state = new
        {
            count = 0,
            isInSilentMode = false
        };

        return new Func<double>(() =>
        {
            state =
                state.count >= desiredSampleLength
                ? new
                    {
                        count = 0,
                        isInSilentMode = false
                    }
                : new
                    {
                        count = state.count + 1,
                        isInSilentMode = state.isInSilentMode
                    };

            return state.isInSilentMode ? 0.0 : 1.0;
        });
    }
    ```

    What we did:
        * mutable state + initialization
        * we update state in a whole pack
        * The factory returns a function that can be evaluated several times in the final computation

### Using the FlipCounter

Since we have a counter, we want to use it in a patch that works this way: Apply a HPF (high pass filter) to a signal for 10 samples, then apply a LPF (low pass filter) to a signal for 10 samples, and so on.

This can be expressed easily in a block diagram:

[TODO: Blockschaltbild]








<!-- 

(******* Exkurs 

[ 0.1; 0.2; 0.3 ]   input
\  | \  | \  |
 \ |  \ |  \ |  --- Processing Function (here: add last and current)
   v    v    v
[  ?; 0.3; 0.5 ]   output

********) -->

