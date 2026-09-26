---
id: machine-07-baremetal
title: Machine 7 — bare metal on QEMU
status: later
stack: [asm, c]
---
No operating system underneath you. An ARM64 program that boots on QEMU's virt machine,
talks to the UART directly, sets up a stack, jumps into C, prints with your own printf,
installs a vector table, and handles a timer interrupt. Done means a tick prints every
second and the README has the memory map and the boot sequence drawn out.

## Steps
- [ ] Install qemu (brew install qemu); boot `qemu-system-aarch64 -M virt -cpu cortex-a57 -nographic -kernel` with a four-line assembly file that spins forever; confirm it runs and that you can quit it (Ctrl-A then x)
- [ ] Write one character to the PL011 UART data register at 0x09000000 from assembly and see it in the terminal
- [ ] A string-printing loop in assembly, with a linker script placing _start at 0x40000000
- [ ] Set the stack pointer and branch into a C kmain; print from C through your own uart_putc
- [ ] Your own printf subset in C (%s %d %x %c) with no libc
- [ ] Read the current exception level with mrs and print it; install a vector table and trigger an SVC whose handler prints a line
- [ ] A timer interrupt through the GIC and the generic timer that prints a tick every second
- [ ] README with the memory map and a boot diagram; push
