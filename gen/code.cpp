#include <string>
#include <iostream>
#include <cstdint>

// type defininions
using u8 = uint8_t;
using u16 = uint16_t;
using u32 = uint32_t;
using u64 = uint64_t;

using i8 =  int8_t;
using i16 = int16_t;
using i32 = int32_t;
using i64 = int64_t;

using f32 = float_t;
using f64 = double_t;

using string = const char*;


// type declarations

// forward declarations
void Main();

// global variables

// function implementations
void Main() {
    for (; true; ) {
        std::cout << "hi" << '\n';
    }
}

// entry point to the program
int main()
{
    // call user main function
    Main();
}

