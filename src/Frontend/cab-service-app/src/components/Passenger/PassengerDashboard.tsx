import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-toastify';
import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet';
import { apiService } from '../../services/apiService';
import { SignalRService } from '../../services/signalRService';
import 'leaflet/dist/leaflet.css';

interface Booking {
  id: string;
  status: string;
  pickupLocation: string;
  dropoffLocation: string;
  fare: number;
  driverName?: string;
  vehicleDetails?: string;
  createdAt: string;
}

const PassengerDashboard: React.FC = () => {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [activeBooking, setActiveBooking] = useState<Booking | null>(null);
  const [loading, setLoading] = useState(true);
  const [userLocation, setUserLocation] = useState<[number, number] | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    loadBookings();
    getCurrentLocation();
    setupSignalRListeners();

    return () => {
      // Cleanup SignalR listeners
      SignalRService.getInstance().off('BookingUpdate', handleBookingUpdate);
      SignalRService.getInstance().off('NewNotification', handleNewNotification);
    };
  }, []);

  const loadBookings = async () => {
    try {
      const userId = localStorage.getItem('userId');
      if (!userId) return;

      const response = await apiService.booking.getByPassenger(userId);
      const bookingData = response.data;
      
      setBookings(bookingData);
      
      // Find active booking
      const active = bookingData.find((b: Booking) => 
        ['Requested', 'Accepted', 'InProgress'].includes(b.status)
      );
      setActiveBooking(active || null);
    } catch (error) {
      console.error('Error loading bookings:', error);
      toast.error('Failed to load bookings');
    } finally {
      setLoading(false);
    }
  };

  const getCurrentLocation = () => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          setUserLocation([position.coords.latitude, position.coords.longitude]);
        },
        (error) => {
          console.error('Error getting location:', error);
          toast.error('Unable to get your location');
        }
      );
    }
  };

  const setupSignalRListeners = () => {
    SignalRService.getInstance().on('BookingUpdate', handleBookingUpdate);
    SignalRService.getInstance().on('NewNotification', handleNewNotification);
  };

  const handleBookingUpdate = (booking: Booking) => {
    setBookings(prev => prev.map(b => b.id === booking.id ? booking : b));
    
    if (['Requested', 'Accepted', 'InProgress'].includes(booking.status)) {
      setActiveBooking(booking);
    } else {
      setActiveBooking(null);
    }

    toast.info(`Booking ${booking.status.toLowerCase()}`);
  };

  const handleNewNotification = (notification: any) => {
    toast.info(notification.message);
  };

  const handleNewBooking = () => {
    navigate('/booking');
  };

  const handleViewTrip = (bookingId: string) => {
    navigate(`/trip/${bookingId}`);
  };

  const handleCancelBooking = async (bookingId: string) => {
    try {
      await apiService.booking.cancel(bookingId, 'Cancelled by passenger');
      toast.success('Booking cancelled successfully');
      loadBookings();
    } catch (error) {
      console.error('Error cancelling booking:', error);
      toast.error('Failed to cancel booking');
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-4">
            <h1 className="text-2xl font-bold text-gray-900">Passenger Dashboard</h1>
            <button
              onClick={() => navigate('/profile')}
              className="bg-blue-500 text-white px-4 py-2 rounded-lg hover:bg-blue-600"
            >
              Profile
            </button>
          </div>
        </div>
      </header>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Active Booking Card */}
        {activeBooking && (
          <div className="bg-white rounded-lg shadow-md p-6 mb-8">
            <h2 className="text-xl font-semibold mb-4 text-green-600">Active Trip</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <p className="text-sm text-gray-600">From</p>
                <p className="font-medium">{activeBooking.pickupLocation}</p>
              </div>
              <div>
                <p className="text-sm text-gray-600">To</p>
                <p className="font-medium">{activeBooking.dropoffLocation}</p>
              </div>
              <div>
                <p className="text-sm text-gray-600">Status</p>
                <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                  activeBooking.status === 'Accepted' ? 'bg-yellow-100 text-yellow-800' :
                  activeBooking.status === 'InProgress' ? 'bg-blue-100 text-blue-800' :
                  'bg-gray-100 text-gray-800'
                }`}>
                  {activeBooking.status}
                </span>
              </div>
              <div>
                <p className="text-sm text-gray-600">Fare</p>
                <p className="font-medium">${activeBooking.fare?.toFixed(2) || 'TBD'}</p>
              </div>
            </div>
            
            {activeBooking.driverName && (
              <div className="mt-4 p-4 bg-gray-50 rounded-lg">
                <p className="text-sm text-gray-600">Driver</p>
                <p className="font-medium">{activeBooking.driverName}</p>
                <p className="text-sm text-gray-500">{activeBooking.vehicleDetails}</p>
              </div>
            )}

            <div className="flex space-x-4 mt-6">
              <button
                onClick={() => handleViewTrip(activeBooking.id)}
                className="bg-blue-500 text-white px-4 py-2 rounded-lg hover:bg-blue-600"
              >
                Track Trip
              </button>
              {activeBooking.status === 'Requested' && (
                <button
                  onClick={() => handleCancelBooking(activeBooking.id)}
                  className="bg-red-500 text-white px-4 py-2 rounded-lg hover:bg-red-600"
                >
                  Cancel
                </button>
              )}
            </div>
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          {/* Quick Actions */}
          <div className="bg-white rounded-lg shadow-md p-6">
            <h2 className="text-xl font-semibold mb-4">Quick Actions</h2>
            <div className="space-y-4">
              <button
                onClick={handleNewBooking}
                disabled={!!activeBooking}
                className={`w-full py-3 px-4 rounded-lg font-medium ${
                  activeBooking 
                    ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
                    : 'bg-green-500 text-white hover:bg-green-600'
                }`}
              >
                {activeBooking ? 'Trip in Progress' : 'Book a Ride'}
              </button>
              
              <button
                onClick={() => navigate('/payment/history')}
                className="w-full py-3 px-4 rounded-lg font-medium bg-blue-500 text-white hover:bg-blue-600"
              >
                Payment History
              </button>
              
              <button
                onClick={() => navigate('/notifications')}
                className="w-full py-3 px-4 rounded-lg font-medium bg-purple-500 text-white hover:bg-purple-600"
              >
                Notifications
              </button>
            </div>
          </div>

          {/* Map */}
          <div className="bg-white rounded-lg shadow-md p-6">
            <h2 className="text-xl font-semibold mb-4">Your Location</h2>
            {userLocation ? (
              <MapContainer
                center={userLocation}
                zoom={13}
                style={{ height: '300px', width: '100%' }}
                className="rounded-lg"
              >
                <TileLayer
                  url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                  attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                />
                <Marker position={userLocation}>
                  <Popup>Your current location</Popup>
                </Marker>
              </MapContainer>
            ) : (
              <div className="h-64 bg-gray-200 rounded-lg flex items-center justify-center">
                <p className="text-gray-500">Loading map...</p>
              </div>
            )}
          </div>
        </div>

        {/* Recent Bookings */}
        <div className="bg-white rounded-lg shadow-md p-6 mt-8">
          <h2 className="text-xl font-semibold mb-4">Recent Trips</h2>
          {bookings.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Route
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Fare
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Date
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {bookings.slice(0, 5).map((booking) => (
                    <tr key={booking.id}>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm text-gray-900">
                          {booking.pickupLocation} → {booking.dropoffLocation}
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                          booking.status === 'Completed' ? 'bg-green-100 text-green-800' :
                          booking.status === 'Cancelled' ? 'bg-red-100 text-red-800' :
                          'bg-yellow-100 text-yellow-800'
                        }`}>
                          {booking.status}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        ${booking.fare?.toFixed(2) || 'N/A'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {new Date(booking.createdAt).toLocaleDateString()}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                        <button
                          onClick={() => handleViewTrip(booking.id)}
                          className="text-blue-600 hover:text-blue-900"
                        >
                          View Details
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="text-gray-500 text-center py-8">No trips yet. Book your first ride!</p>
          )}
        </div>
      </div>
    </div>
  );
};

export default PassengerDashboard;
