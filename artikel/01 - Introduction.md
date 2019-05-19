
## Introduction

### Motivation

Making music with the help of computers is not new. It started with the [MUSIC/MUSIC-N](https://en.wikipedia.org/wiki/MUSIC-N) language family, which was already capable of synthesizing sounds, back in the 1950s. There are a lot of graphic tools like [PureData](https://puredata.info/), with which the user can put together signal flows by dragging predefined objects onto a canvas and connecting their inputs/outputs. Some tools focus on controlling, composition and live performance, like [Sonic PI](https://sonic-pi.net/) or [Tidal](https://tidalcycles.org/index.php/Welcome), while others can do both, like [SuperCollider](https://supercollider.github.io/). Other languages and tools incorporate interesting concepts to make working with (audio and control) signals easy, like [ChucK](http://chuck.cs.princeton.edu/), which has an interesting way of dealing with time. There is the [FAUST](https://faust.grame.fr/) language that makes use of a pure function composition concept quite similar to Haskell's [Arrows](https://en.wikibooks.org/wiki/Haskell/Understanding_arrows). And of course, with the widely used multiparadigm languages like C or C++, the user can do basically everything, as long as he or she knows how to do it. There is way more interesting stuff out there, such as the JavaScript library [genish.js / gibberish](http://www.charlie-roberts.com/genish/), which aims to provide ways by which a user can define a syntax tree that is translated into high performance code.

Because developing software is my profession and my hobby, I wanted to understand why there are so many different tools and languages out there. I started my own work in C#, since this is the main language I work in, with a focus on sample-based generation and processing of audio signals (the code is available [here](https://archive.codeplex.com/?p=byond), but it is not maintained anymore). But I soon began to realize that there are disadvantages: I had to deal with aspects that had nothing to do with describing signal flows, and I had to write a lot more boilerplate code. That all smelled like [accidental complexity](https://en.wikipedia.org/wiki/No_Silver_Bullet), and I asked myself how I could find abstractions that would help me write code that was readable and understandable, and that focused solely on signal processing.

After evaluating several languages, I decided to use F#, mainly due to its "in-built" flexibility without dealing with macros or other hardcore meta programming techniques or compiler hooks (although I find them very interesting). Also, I'm quite familiar with .NET, but that turned out to be not important at all. One thing I can state is that I learned a lot, and I have never regretted my choice to use F#.

### What is DSP?

#### A Brief Definition

In the analog world, physical quantities like electric currency are used to represent a signal that is finally sent to a speaker. These quantities are created and altered by low-level components like condensers, resistors, magnetic coils, transistors, and diods, which are connected to each others in circuits. They are composed in larger components like operational amplifiers that are used to build modules like filters and oscillators, which synthesizers and effect processors are made of. Digital audio signal processing (Audio DSP) is about modeling these components at different levels of abstraction, resulting in the simulation of circuits similar to their analog counterparts (of course, there is no limit to creating completely new forms of synthesizers and effects).

#### Quantization of Time and Values

Digital signal processing - in contrast to analog signal processing - deals with quantized values over a discrete time. Consider, for instance, an analog synthesizer. It creates and outputs a signal based on electric currency, which is continuous from a physical point of view. A computer cannot process values in a continuous way - it has to _quantize_ two things:

**Time:**

This is called sampling, and it happens for an audio signal usually at multiples of 44100 times per second (44.1 kHz). Why 44100? Take a look at the [Nyquist-Shannon sampling theorem](https://en.wikipedia.org/wiki/Nyquist-Shannon_sampling_theorem) (you do not have to read the Wikipedia article it to understand this article).

**Values:**

At each sample point, a value must be captured (analog to the electric currency). This happens usually in a number represented by a 16, 32 or 64 bit value. In this article, I use a F# "float" value, which is a 64 bit floating point number between `0.0` and `1.0` (normed to 0% .. 100% amplitude).

Having understood this definition, it's easy to define what a signal is.

<statement>A signal is a value that changes over time.</statement>

Sampling these values with constant time intervals results in a sequence of values.

![Sin Wave](./sinus_wave.png)

Here, I have captured a sine wave with the _amplitude_ of 0.5 and a frequency of ca. 150Hz (assuming the x-scale is milliseconds, then I have three cycles in 0.02s = 150Hz). The sample rate is 1kHz because I have captured 20 samples in 0.02s. That makes 1000 samples in 1s. This is 1kHz.

Given that I have a sample rate of 16Hz, I can just a value sequence instead of a time-value sequence:

```fsharp
[ 0.0; 0.4047782464; 0.4752436455; 0.1531976936; -0.2953766911; -0.499994728; (*and so on *) ]
```

The point in time of the the n-th value in the sequence can easily be calculated when sample rate and starting time are given. This fact is fundamental and leads to a definition of what _processing_ means:

<statement>DSP is about creating or changing sequence of values.</statement>

That sounds very general - and it indeed is! The techniques introduced here have basically no specialization in terms of "sound" or "audio" - even if they fit well in that domain.

<hint>

For the sake of simplification, please note that the sample code won't use real-world parameters like `8000.0Hz`. Instead, pseudo values are used to create a comprehensive result and keep the code simple.

</hint>

#### Real Time

"Real Time" originally means that a system is able to react in a predefined timespan. That does not necessarily mean it has to be "fast" in the context of the problem, but only that it is reliable in terms of reaction time. Since making music mostly has a "live" character, this is a huge constraint that affects how computer music is made. An example: You composed a nice synth line, and now you want to apply your handwritten distortion effect to it. While it is playing, you want to tune parameters of your effect - and you expect to hear a change in sound _immediately_, which usually means in some 10 to 100 ms. When that timespan is longer, IMHO, it's not that fun anymore and is also harder because you are missing the direct feedback of your action.

As a consequence, you have to design systems that work on a per-sample basis instead of having random access to the whole input sequence. This means it is not possible to wait until the whole input signal (the synth line) is available and then apply your effect by mapping values. That amount of latency wouldn't be acceptable for the _most_ use cases. Therefore, signal processing can be seen as some kind of _stream processing_.

### Distinction

Audio DSP and computer music are broad fields. This article focuses on some aspects while only touching others or not discussing others. Here, I will list the key concepts and distinctions of this article.

* The focus is on creating and manipulating samples that finally may result in sounds, rather than on control signals or composition. It also does not discuss libraries for audio playback.
* The article focuses on monophonic mono signals, although the concepts allow polyphony and multichannel signals.
* There are no performance considerations in this article for the introduced concepts.
* Signals are represented in the time domain, not in the frequency domain. 

### F# Getting Started

F# is a concise and easy to learn language, since it has only a few, but powerful, key concepts and an easy to understand syntax. If you have never dealt with a language of the ML family, I recommend these sources for getting started with F#.

* [F# Cheat Sheet](http://dungpa.github.io/fsharp-cheatsheet/)
  
If[SGF16] you are familiar with a modern language like C#, Java, C++, JavaScript, TypeScript, Python (or whatever), this might be a good source for a kick start.

* [F# for Fun and Profit (Scott Wlaschin)](https://fsharpforfunandprofit.com/)

This was my primary source when I  was getting involved with F#, and I can definitely recommend it when you want to learn about the key concepts of functional programming. I recommend reading [this](https://fsharpforfunandprofit.com/series/expressions-and-syntax.html), [this](https://fsharpforfunandprofit.com/posts/elevated-world-2/), and [this](https://fsharpforfunandprofit.com/posts/currying/#series-toc).

* The book _Real-World Functional Programming: With Examples in F# and C#_ by Tomas Petricek.

* The book _Expert F# 4.0_ by Don Syme et al. I especially recommend chapters 2 and 3, which capture the most common functional aspects of F#.

### Setup and Samples

A lot of code is presented in this article. Since no one has to believe that the things I'm telling here are true, you can easily reproduce and comprehend them by your own.

I recommend an easy setup consisting of the following tools.

* **Visual Studio Code**
  
VS Code is a lightweight and free to use editor. Get it [here](https://code.visualstudio.com/)!

* **Ionide**
  
Ionide is a VS Code package suite for cross platform F# development. You can get it [here](http://ionide.io/).

On the Ionide homepage, you can see how to install F# for your platform (macOS, Windows, Linux).

* **F# Interactive**

I recommend making yourself familiar with the concept of _interactive development_, which is called "F# Interactive". It is a playground for evaluating code snippets that are built upon each other, without setting up a whole development project. The concept and tools are well explained in the reference list above. Using VS Code and Ionide, you have everything you need to get started immediately.

#### Article Sources

There is a [github repository](https://github.com/ronaldschlenker/challenge) that you can clone (or view online) with all the samples included.

Now you are equipped with everything you need, so let's get our hands into it!
