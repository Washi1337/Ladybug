// TestMultiThreadedApplication.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <thread>
#include <iostream>

void thread1() 
{
	for (long long i = 0; i < LLONG_MAX; i++)
	{
		std::cout << "Thread1: " << i << std::endl;
		std::this_thread::sleep_for(std::chrono::seconds(1));
	}

}

void thread2()
{
	for (long long i = LLONG_MAX; i >= 0; i--)
	{
		std::cout << "Thread2: " << i << std::endl;
		std::this_thread::sleep_for(std::chrono::seconds(1));
	}
}


int main()
{
	std::cout << "main: " << main << std::endl;
	std::cout << "thread1: " << thread1 << std::endl;
	std::cout << "thread2: " << thread2 << std::endl;

	std::cout << std::endl;

	std::thread t1(thread1);
	std::thread t2(thread2);

	t1.join();
	t2.join();
    return 0;
}

