
## Writing Stateful Functions

In the previous chapters, we wrote and composed pure (stateless) functions. This means that processing a signal (= a sequence of values) was made up of the following "recipe" (see chapter 1):

* We have a sequence of input values.
* We have processing functions.
* We can compose these functions in several ways.
* These functions are curried, so that after application of all parameters values, functions of ```float -> float``` remain.
* We have a "runtime" that generates an output sequence by mapping each value in the input sequence to a value in the output sequence by applying a processing function to an the input value.

### Revisit Low Pass Filter

In the last chapter, we treated the "Low Pass Filter" function as if it was pure, which means: From evaluation cycle to cycle, it doesn't "remember" anything; there is no information preserved between evaluations of the same function. In case of a filter, this cannot work, because filters need more than a single value: They deal with frequencies, and the concept of frequency requires a timespan: It's about how a sequence of values change ofer time. It's like with stock prices: You cannot say if there was a crash or if the market is hot by just looking at the current value. You need to look at the development of a price during a certain timespan. And, there are more things: Some filters only need some past input values (FIR; finite impulse response). But there are other filter designs that depend on their past output values (IIR; infinite impulse response). So we need a mechanism that preserves past input, past output (and maybe past intermediate) **state**.

<excurs data-name="Very Brief theory of a low pass filter">

To understand what this means, we look at how a low pass filter can be designed.

The most simple way of designing an electronic low pass is using a resistor and a condensor in series, which is then connected to a currency that represents the input signal (`Ue`). The output signal is the currency of the condensor (`Ua`). So why is that a low pass filter?

![Resistor Condenser](./rc_glied.png)

**Resistor:**

It works like a valve in a water pipe: it limits the possibility of electrons to flow around. So when you connect the poles of a battery to each other, you will get a shortcut, because the electrons can go from one pole to the other without any obstacle (and releasing the battery's energy in a very short amount of time). But if you connect the poles to a resistor (e.g. a glowing lamp), the electron flow is limited (thus releasing the energy in a much longer timespan).

**Condensor:**

Basically, it works like a battery - it can store and release energy - only much faster. It is made up of 2 poles (e.g. metal plates). Each plate can be charged up with a certain amount of electrons; the more electrons it has, the higher the currency measured between the 2 plates. Connecting a currency to each plate, the electrons will flow from the 1 pole of the currency to the connected plate of the condensor, and the electrons from the other condencor plate will flow to the other pole of the currency. _After some time_ (which depends on the dimension of the resistor), the condensor has the same voltage as the currency, and it is then fully loaded. If you switch the connected currency poles (+ <> -), the condensor will first unload and then load again with switched voltages, until it is in balance with the currency again.

Since the currency represents our input signal, we can say:

* High frequency is a fast change of currency.
* High frequency is a slow change of currency.

So when the input currency changes very quickly (high frequency), the condensor has not enough time to fill itself up with electrons, and if we measure it's voltage, it will be almost zero. When the input currency changes slowly, the condensor has time for it's load and unload cycle, so we will be able to measure a voltage (which equal approximately the input signal when the input frequency change is 0).

And that's out low pass filter: Low input frequencys can be measured at the condensor output, high frequencies have no effect on the measured condensor output voltage.

The key point for this is: The condensor brings the time into the game! It has state, which is made up of the "current electron load". The next moment's output value is made up of the last moment's internal value and a current external input value.  

</excurs>

How can that be modeled?

#### State in the block diagram

Let's describe the characteristics of this low pass filter in a textual way:

1. An instance of low pass filter has state that represents the load amount. In our case, that state is proportional to the output value (like the amount of electrons is proportional to the output currency of the condensor).

2. The "calculation recipe" for the output value from one moment to the next moment is this:
    * Take the output value from the last evaluation (which we call `lastOut`).
    * Take the difference between `lastOut` and the `current input`.
    * Multiply that difference with a given time constant (to slow down the load time and thus adjusting the filter's frequency).
    * The current output value is that difference subtracted from lastOut.

That's it. Think about it: The bigger the difference between current input and the last output value, the faster the condensor "loads up". The smaller the time constant gets, the slower the system reacts to input value changes - a low pass filter!

Let's see how we can implement it in a block diagram:

![Low pass filter](./lpf_inside.png)

One interesting thing to notice: There is no explicit state in the way that we store or memorize values. Instead, the state is modeled as "output value delayed by 1 sample" ("t-1" block), which is then fed back into the next evaluation of the whole function. **This is a key point** because we can model any kind of local state in that way - no matter how that state is structured (it doesn't have to be a simple `float`- it could be anything). A abstract "block with state" can then be modeled like this:

![Block with state and parameters](./block_with_state_and_params.png)

Beside the output value, there is an output state. And beside the input value, there comes an input state that is the output state from the last evaluation (plus curried function parameters - as usual).

In the following chapters, we will have a look on ways of writing and composing such functions, and we will understand that these ways provide less or more compfort for the user who wants to express signal processing computations.

We start with an object oriented programming approach.
