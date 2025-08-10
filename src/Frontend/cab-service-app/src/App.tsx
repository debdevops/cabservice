import React from 'react';
import './App.css';

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <h1 className="text-4xl font-bold text-blue-600 mb-4">
          Cab Service
        </h1>
        <p className="text-lg text-gray-700 mb-8">
          Your reliable ride booking platform
        </p>
        <div className="space-y-4">
          <button className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded">
            Book a Ride
          </button>
          <button className="bg-green-500 hover:bg-green-700 text-white font-bold py-2 px-4 rounded ml-4">
            Become a Driver
          </button>
        </div>
      </header>
    </div>
  );
}

export default App;
