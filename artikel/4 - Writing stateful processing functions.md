
## Writing stateful processing functions

In the previous chapters, we wrote and composed pure (stateless) functions. This means that processing a signal (= a sequence of values) was made up of the following "recipe" (see chapter 1):

    * We have a sequence of input values.
    * We have a processing function.
    * That function is curried, so that after application of all parameters values, a function of float->float remains.
    * We have a "runtime" that generates an output sequence by mapping each value in the input sequence to a value in the output sequence by applying the processor function to an the input value.

### Revisit Low Pass Filter

In the last chapter, we treated the "Low Pass Filter" function as if it was pure, which means: From evaluation cycle to cycle, it doesn't "remember" anything; there is no information preserved between evaluations of the same function. In case of a filter, this cannot work, because filters need more than a single value: They deal with frequencies, and the concept of frequency requires a timespan: It's about how a sequence of values change ofer time. It's like with stock prices: You cannot say if there was a crash or if the market is hot by just looking at the current value. You need to look at the development of a price during a certain timespan. And, there are more things: Some filters only need some past input values (FIR; finite impulse response). But there are other filter designs that depend on their past output values (IIR; infinite impulse response). So we need a mechanism that preserves past input, past output (and maybe past intermediate) values.

#### Very Brief theory of a low pass filter

The most simple way of designing an electronic low pass is using a resistor and a condensor in series, which is then connected to a currency that represents the input signal. The output signal is the currency of the condensor. So why is that a low pass filter?

TODO: Bild

First, the resistor: It works like a valve in a water pipe: it limits the possibility of electrons to flow around. So when you connect the poles of a battery to each other, you will get a shortcut, because the electrons can go from one pole to the other without any obstacle (and releasing the battery's energy in a very short amount of time). But if you connect the poles to a resistor (e.g. a glowing lamp), the electron flow is limited (thus releasing the battery'S energy in a much longer timespan).

Second, the condensor: Basically, it works like a battery - it can store and release energy - only much faster. It is made up of 2 poles (e.g. metal plates). Each plate can be charged up with a certain amount of electrons; the more electrons it has, the higher the currency measured between the 2 plates. Connecting a battery to each plate, the electrons will flow from the 1 pole of the battery to the connected plate of the condensor, and the electrons from the other condencor plate will flow to the other pole of the battery. _After some time_ (which depends on the dimension of the resistor), the condensor has the same currency as the battery, and it is then fully loaded. If you switch the connected battery poles (+ <> -), the condensor will first unload and then load again with switched currency direction, until it is in balance with the battery again.

Since the currency represents our input signal, we can say:

* High frequency is a fast change of currency.
* High frequency is a slow change of currency.

So when the input currency changes very quickly (high frequency), the condensor has not enough time to fill itself up with electrons, and if we measure it's currency, it will be almost zero. When the input currency changes slowly, the condensor has time for it's load and unload cycle, so we will be able to measure a currency (which equal approximately the input signal when the input frequency change is 0).

And that's out low pass filter: Low input frequencys can be measured at the condensor output, high frequencies have no effect on the measured condensor output currency.

The key point for this is: The condensor brings the time into the game! It has state, which is made up of the "current electron load". The next moment's output value is made up of the last moment's internal value and a current external input value.  

How can that be modeled?

#### State in the block diagram

Let's describe the characteristics of this low pass filter in a textual way:

* An instance of low pass filter has state that represents the load amount. In our case, that state is proportional to the output value (like the amount of electrons is proportional to the output currency of the condensor).
* Calculation recipe for the output value from one moment to the next moment is:
    * Take the output value from the last evaluation (which we call "lastOut").
    * Take the difference between lastOut and the current input.
    * Multiply that difference with a given time constant (to slow down the load time and thus adjusting the cutoff frequency).
    * The current output value is that difference subtracted from lastOut.

That's it. Think about it: The bigger the difference between current input and the last output value, the faster the condensor "loads up". The smaller the time constant gets, the slower the system reacts to input value changes - a low pass filter!

Let's see how we can implement it in a block diagram:

[lpf_inside.tif]

One interesting thing to notice: There is no state explicitly in a way that we store or memorize values. Instead, the state is modeled as the output value delayed by 1 sample ("t-1" block), which is then fed back into the next evaluation of the whole function. **This is a key point** because we can model any kind of local state in that way - no matter how they are structured or calculated. A abstract "block with state" can then be modeled like this:

[block_with_state_and_params.tif]

Beside the output value, there is an output state. And beside the input value, there comes an input state that is the output state from the last evaluation (plus curried function parameters - as usual).

In the following chapters, we will have a look on ways of composing such functions, and we will understand that these ways provide less or more compfort for the user who wants to express signal computations.

We start with an object oriented programming approach.

<!-- 


TODO:
    * Kondensator modellieren mit Rückkopplung
    * Rückkopplung ist "intern" - Kasten drum; black box
    * Dann: Verwendung


OOP:
    * Es ist ok, das so mit mutable zu schreiben.
    * Aber: Die Verwendung ist doof, weil: Wir _brauchen_ eine Referenz.
        * Identity in imperative lang is made by an address. Accessing the address is made by a name.
        * BlockDiag: Identity (of the concrete LP filter instance) is made by it's location in the computation. -->


<!-- 
Now there are ceratin ways to deal with this:

#### Using lists of floats

Instead of having processing functions that transform a single sample ("float -> float"), we could use functions that operate on a whole sequence of samples (in the form of "seq<float> -> seq<float>"). In that case, a processing function could use 

TODO: Probleme:
    * Latency
    * endless signals: How long to wait?
        * -> Limit 
 -->



















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